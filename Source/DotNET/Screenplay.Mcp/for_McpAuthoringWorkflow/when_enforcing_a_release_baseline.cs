// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_enforcing_a_release_baseline : given.an_authoring_connection
{
    const string Source = "concept Value : String\nconcept Status : Enum\n  draft = 1\n  sent = 4\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        value Value\n        status Status\n";
    ScreenplayWorkspace _model = null!;

    [Theory]
    [InlineData("source")]
    [InlineData("ast")]
    void should_refuse_a_shared_concept_change_on_every_transport(string transport)
    {
        var opened = Start();
        Admit();
        var original = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        var arguments = transport == "source" ? SourceArguments(opened, Source.Replace("Value : String", "Value : Int", StringComparison.Ordinal)) : AstArguments(opened, "ConceptSyntax", "Value", new { kind = "ConceptSyntax", name = "Value", type = "Int", attributes = Array.Empty<string>(), values = Array.Empty<string>() });
        var refusal = Failure(transport == "source" ? "propose-source" : "propose-ast", arguments);
        refusal.GetProperty("failureKind").GetString().ShouldEqual("BaselinedRevisionChanged");
        refusal.GetProperty("violations").GetArrayLength().ShouldEqual(1);
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(original);
    }

    [Fact]
    void should_refuse_an_enum_addition_under_a_frozen_revision()
    {
        var opened = Start();
        Admit();
        Failure("propose-source", SourceArguments(opened, Source.Replace("sent = 4", "sent = 4\n  archived = 8", StringComparison.Ordinal))).GetProperty("failureKind").GetString().ShouldEqual("BaselinedRevisionChanged");
    }

    [Theory]
    [InlineData(false, "BaselinedRevisionImmutable")]
    [InlineData(true, "BaselinedRevisionImmutable")]
    void should_refuse_in_place_event_property_edits(bool neverPersisted, string failure)
    {
        var opened = Start();
        Admit();
        Failure("propose-property", PropertyArguments(opened, neverPersisted)).GetProperty("failureKind").GetString().ShouldEqual(failure);
    }

    [Fact]
    void should_report_unadmitted_in_place_mutability_in_the_retained_plan()
    {
        var opened = Start();
        Admit(empty: true);
        var proposal = Result("propose-property", PropertyArguments(opened));
        var generation = proposal.GetProperty("property").GetProperty("generations")[0];
        generation.GetProperty("mode").GetString().ShouldEqual("inPlace");
        generation.GetProperty("mutability").GetProperty("basis").GetString().ShouldEqual("notInBaseline");
        var review = Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "release-baseline" });
        review.GetProperty("property").GetProperty("generations")[0].GetRawText().ShouldEqual(generation.GetRawText());
        review.GetProperty("result").GetProperty("page").GetProperty("items")[0].GetProperty("mutability").GetProperty("basis").GetString().ShouldEqual("notInBaseline");
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_refuse_apply_after_admission_without_changing_source()
    {
        var opened = Start();
        var original = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        var proposal = Result("propose-source", SourceArguments(opened, Source + "// reviewed\n"));
        Admit();
        Failure("apply", ApplyArguments(opened, proposal)).GetProperty("failureKind").GetString().ShouldEqual("StaleReleaseBaseline");
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(original);
    }

    [Fact]
    void should_accept_an_optional_baseline_pin_and_refuse_a_wrong_one()
    {
        var opened = Start();
        Admit();
        var arguments = JsonSerializer.SerializeToNode(SourceArguments(opened, Source + "// reviewed\n"))!.AsObject();
        arguments["expectedReleaseBaselineRevision"] = "brev1:absent";
        Failure("propose-source", arguments).GetProperty("failureKind").GetString().ShouldEqual("StaleReleaseBaseline");
        arguments["expectedReleaseBaselineRevision"] = Open().GetProperty("releaseBaseline").GetProperty("revision").GetString();
        Result("propose-source", arguments).GetProperty("success").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_open_read_and_review_an_unreadable_baseline_but_refuse_contract_edits()
    {
        var opened = Start();
        File.WriteAllText(BaselinePath(), "<<<<<<< main\n=======\n>>>>>>> branch");
        opened = Open();
        opened.GetProperty("releaseBaseline").GetProperty("status").GetString().ShouldEqual("unreadable");
        Result("workspace-state").GetProperty("releaseBaseline").GetProperty("status").GetString().ShouldEqual("unreadable");
        Result("list-release-baseline").GetProperty("releaseBaseline").GetProperty("status").GetString().ShouldEqual("unreadable");
        var proposal = Result("propose-source", SourceArguments(opened, Source + "// review is still possible\n"));
        Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "release-baseline" }).GetProperty("result").GetProperty("releaseBaseline").GetProperty("status").GetString().ShouldEqual("unreadable");
        Failure("propose-source", SourceArguments(opened, Source.Replace("Value : String", "Value : Int", StringComparison.Ordinal))).GetProperty("failureKind").GetString().ShouldEqual("UnreadableReleaseBaseline");
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_page_admitted_entries_and_report_current_differences()
    {
        Start();
        Admit();
        var first = Result("list-release-baseline", new { limit = 1 });
        first.GetProperty("page").GetProperty("items")[0].GetProperty("status").GetString().ShouldEqual("unchanged");
        var revision = first.GetProperty("releaseBaseline").GetProperty("revision").GetString();
        Result("list-release-baseline", new { offset = 1, expectedReleaseBaselineRevision = revision }).GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(0);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("Value : String", "Value : Int", StringComparison.Ordinal));
        Open().GetProperty("releaseBaseline").GetProperty("violations").GetInt32().ShouldEqual(1);
        var changed = Result("list-release-baseline").GetProperty("page").GetProperty("items")[0];
        changed.GetProperty("status").GetString().ShouldEqual("changed");
        changed.GetProperty("path").GetString().ShouldNotBeNull();
    }

    [Fact]
    void should_apply_notes_on_a_nonbinding_model_with_a_valid_baseline()
    {
        Start();
        Admit();
        const string source = "numbers exact\n" + Source;
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        var opened = Open();
        opened.GetProperty("releaseBaseline").GetProperty("checkFailure").GetString().ShouldEqual("ModelDoesNotBind");
        var identities = File.ReadAllBytes(Path.Combine(RootPath, ".screenplay", McpState.FileName));
        var proposal = Result("propose-model-notes", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            expectedNotesRevision = opened.GetProperty("notesRevision").GetString(),
            operations = new[] { new { operation = "add", kind = "task", text = "Review execution readiness.", author = "sindre", anchor = new { application = true } } }
        });
        Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "release-baseline" }).GetProperty("result").GetProperty("releaseBaseline").GetProperty("checkFailure").ValueKind.ShouldEqual(JsonValueKind.Null);
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
        File.ReadAllBytes(Path.Combine(RootPath, ".screenplay", McpState.FileName)).ShouldEqual(identities);
        File.Exists(Path.Combine(RootPath, ".screenplay", "notes.json")).ShouldBeTrue();
    }

    [Fact]
    void should_propose_and_apply_a_noncontract_edit_on_a_nonbinding_model()
    {
        Start();
        Admit();
        const string source = "numbers exact\n" + Source;
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        var opened = Open();
        var proposal = Result("propose-source", SourceArguments(opened, source + "// reviewed\n"));
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
    }

    JsonElement Start()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var opened = Open();
        var proposal = Result("propose-source", SourceArguments(opened, Source));
        _model = Candidate(proposal);
        return Apply(opened, proposal).GetProperty("workspace");
    }

    void Admit(bool empty = false)
    {
        var baseline = ReleaseBaseline.Admit(_model, ReleaseBaselineStore.ReadBytes(null, _model.IdentityCatalog.Application), "v1", DateTimeOffset.UtcNow).Document!;
        File.WriteAllBytes(BaselinePath(), ReleaseBaselineStore.Serialize(empty ? baseline with { Entries = [] } : baseline));
    }

    string BaselinePath() => Path.Combine(RootPath, ".screenplay", ReleaseBaselineStore.FileName);

    object SourceArguments(JsonElement opened, string source) => new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        documents = new[] { new { operation = "replace-document", documentId = _model?.Documents[0].Id.ToString() ?? Page("documents", opened.GetProperty("revision").GetString()!)[0].GetProperty("documentId").GetString(), source } }
    };

    object AstArguments(JsonElement opened, string kind, string name, object node) => new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        formatting = "CanonicalizeTouchedDocuments",
        operations = new[] { new { operation = "replace", target = Result("read-ast", new { expectedRevision = opened.GetProperty("revision").GetString(), kind, name }).GetProperty("page").GetProperty("items")[0].GetProperty("handle"), node } }
    };

    object PropertyArguments(JsonElement opened, bool neverPersisted = false) => new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        action = "add",
        owner = Node("EventSyntax", opened.GetProperty("revision").GetString()!).GetProperty("handle"),
        expectedOwnerName = "Registered",
        name = "note",
        type = "String",
        optional = true,
        eventNeverPersisted = neverPersisted,
        formatting = "CanonicalizeTouchedDocuments"
    };

    static object ApplyArguments(JsonElement opened, JsonElement proposal) => new { expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(), proposalId = proposal.GetProperty("proposalId").GetString() };

    JsonElement Failure(string tool, object arguments)
    {
        var result = Call(tool, arguments).GetProperty("result");
        Assert.True(result.GetProperty("isError").GetBoolean(), result.GetRawText());
        return result.GetProperty("structuredContent");
    }
}

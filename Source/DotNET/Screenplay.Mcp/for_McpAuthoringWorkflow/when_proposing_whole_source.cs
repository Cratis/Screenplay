// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_proposing_whole_source : given.an_authoring_connection
{
    const string Streams = "concept Month : Int\neventsource Account\n  stream Transactions\n    streamId Month\n";

    [Fact]
    void should_create_syntax_only_source_with_authoring_validation_by_default()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
        var opened = Open();
        var proposal = Result("propose-source", Arguments(opened, new { operation = "create-document", path = "application.play", stableKey = "application", source = Streams }));
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
        proposal.GetProperty("after").GetProperty("executableReady").GetBoolean().ShouldBeFalse();
        proposal.GetProperty("introducedExecutableErrors").GetRawText().ShouldContain("PLAY0268");
        var review = Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "changes" });
        review.GetRawText().ShouldContain("application.play");
        File.Exists(Path.Combine(RootPath, "application.play")).ShouldBeFalse();
        Apply(opened, proposal);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("stream Transactions");
    }

    [Fact]
    void should_refuse_syntax_only_source_under_executable_validation()
    {
        Initialize();
        var opened = Open();
        var refused = Failure(Arguments(opened, new { operation = "create-document", path = "streams.play", stableKey = "streams", source = Streams }, "Executable"));
        refused.GetProperty("executableDiagnostics").GetRawText().ShouldContain("PLAY0268");
        refused.TryGetProperty("proposalId", out _).ShouldBeFalse();
    }

    [Fact]
    void should_preserve_assigned_semantic_ids_on_source_replacement()
    {
        Initialize();
        var opened = Open();
        var proposal = Result("propose-source", Arguments(opened, Replace(opened, Source)));
        Apply(opened, proposal);
        opened = Open();
        var before = Page("semantics", opened.GetProperty("revision").GetString()).GetRawText();
        proposal = Result("propose-source", Arguments(opened, Replace(opened, Source.Replace("Registers a new project", "Registers a café project", StringComparison.Ordinal))));
        Apply(opened, proposal);
        var after = Page("semantics", Open().GetProperty("revision").GetString()).GetRawText();
        after.ShouldEqual(before);
    }

    [Fact]
    void should_refuse_dropping_an_assigned_declaration_without_retirement()
    {
        Initialize();
        var opened = Open();
        Apply(opened, Result("propose-source", Arguments(opened, Replace(opened, Source))));
        opened = Open();
        var refused = Failure(Arguments(opened, Replace(opened, "concept ProjectId : Uuid\nconcept ProjectName : String\n")));
        refused.GetProperty("conflicts").GetRawText().ShouldContain("Identity");
        refused.TryGetProperty("proposalId", out _).ShouldBeFalse();
    }

    [Fact]
    void should_return_located_parse_errors_without_retaining_a_proposal()
    {
        Initialize();
        var opened = Open();
        var refused = Failure(Arguments(opened, Replace(opened, "module Projects\n  not-a-declaration\n")));
        refused.GetProperty("failureKind").GetString().ShouldEqual("SourceParseFailed");
        var diagnostic = refused.GetProperty("authoringDiagnostics")[0];
        diagnostic.GetProperty("location").GetProperty("path").GetString().ShouldEqual("application.play");
        diagnostic.GetProperty("location").GetProperty("line").GetInt32().ShouldBeGreaterThan(0);
        diagnostic.GetProperty("location").GetProperty("column").GetInt32().ShouldBeGreaterThan(0);
        refused.TryGetProperty("proposalId", out _).ShouldBeFalse();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    }

    [Fact]
    void should_parse_created_fragments_in_the_final_import_placement()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
        var opened = Open();
        var proposal = Result("propose-source", Arguments(opened,
        [
            new { operation = "create-document", path = "slice.play", stableKey = "slice", source = "slice StateChange Register\n  event Registered\n" },
            new { operation = "create-document", path = "application.play", stableKey = "application", source = "module Sales\n  feature Orders\n    import \"slice.play\"\n" }
        ]));
        Apply(opened, proposal);
        var slice = Node("SliceSyntax", Open().GetProperty("revision").GetString());
        slice.GetProperty("location").GetProperty("path").GetString().ShouldEqual("slice.play");
    }

    [Fact]
    void should_require_explicit_formatting_consent()
    {
        Initialize();
        var opened = Open();
        var response = Call("propose-source", new { expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(), documents = new[] { Replace(opened, Source) } });
        response.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    }

    [Fact]
    void should_parse_routes_using_unchanged_source_declarations()
    {
        File.WriteAllText(Path.Combine(RootPath, "sources.play"), Streams);
        Initialize();
        var opened = Open();
        var proposal = Result("propose-source", Arguments(opened, new { operation = "create-document", path = "deposits.play", stableKey = "deposits", source = "module Banking\n  feature Deposits\n    slice StateChange Deposit\n      command Deposit\n        month Month\n        stream Account.Transactions\n          streamId = month\n" }));
        Candidate(proposal).Compilation.Success.ShouldBeFalse();
        Apply(opened, proposal);
        Node("CommandStreamSyntax", Open().GetProperty("revision").GetString()).GetProperty("node").GetProperty("stream").GetString().ShouldEqual("Transactions");
    }

    [Fact]
    void should_preserve_bom_policy_on_created_and_replaced_documents()
    {
        Initialize();
        var opened = Open();
        var proposal = Result("propose-source", Arguments(opened, new { operation = "create-document", path = "streams.play", stableKey = "streams", encoding = "Utf8WithBom", source = Streams }));
        Apply(opened, proposal);
        opened = Open();
        var document = Page("documents", opened.GetProperty("revision").GetString()).EnumerateArray().Single(document => document.GetProperty("path").GetString() == "streams.play");
        proposal = Result("propose-source", Arguments(opened, new { operation = "replace-document", documentId = document.GetProperty("documentId").GetString(), source = Streams }));
        Apply(opened, proposal);
        File.ReadAllBytes(Path.Combine(RootPath, "streams.play")).Take(3).ShouldContainOnly((byte)0xef, (byte)0xbb, (byte)0xbf);
    }

    [Fact]
    void should_reject_stale_revisions_before_parsing_source()
    {
        Initialize();
        var opened = Open();
        var response = Call("propose-source", new
        {
            expectedRevision = "wsrev1:" + new string('0', 64), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            documents = new[] { new { operation = "create-document", path = "bad.play", stableKey = "bad", source = "invalid" } }
        }).GetProperty("result").GetProperty("structuredContent");
        response.GetProperty("failureKind").GetString().ShouldEqual("StaleRevision");
        response.TryGetProperty("proposalId", out _).ShouldBeFalse();
    }

    [Fact]
    void should_move_rename_and_remove_source_documents_with_the_same_identity()
    {
        File.WriteAllText(Path.Combine(RootPath, "streams.play"), Streams);
        Initialize();
        var opened = Open();
        var id = Page("documents", opened.GetProperty("revision").GetString()).EnumerateArray().Single(document => document.GetProperty("path").GetString() == "streams.play").GetProperty("documentId").GetString();
        Apply(opened, Result("propose-source", Arguments(opened, new { operation = "move-document", documentId = id, path = "sources.play" })));
        opened = Open();
        Apply(opened, Result("propose-source", Arguments(opened, new { operation = "rename-document-key", documentId = id, stableKey = "sources" })));
        opened = Open();
        Page("documents", opened.GetProperty("revision").GetString()).EnumerateArray().Single(document => document.GetProperty("path").GetString() == "sources.play").GetProperty("documentId").GetString().ShouldEqual(id);
        Apply(opened, Result("propose-source", Arguments(opened, new { operation = "remove-document", documentId = id })));
        File.Exists(Path.Combine(RootPath, "sources.play")).ShouldBeFalse();
        File.Exists(Path.Combine(RootPath, "streams.play")).ShouldBeFalse();
    }

    [Fact]
    void should_admit_explicit_retirements_through_the_shared_transaction()
    {
        Initialize();
        var opened = Open();
        Apply(opened, Result("propose-source", Arguments(opened, Replace(opened, Source))));
        opened = Open();
        var arguments = (Dictionary<string, object?>)Arguments(opened, Replace(opened, "concept ProjectId : Uuid\nconcept ProjectName : String\n"));
        var revision = opened.GetProperty("revision").GetString();
        arguments["retiredSemanticAddresses"] = Page("semantics", revision).EnumerateArray().Where(assignment => assignment.GetProperty("address").GetProperty("parts").EnumerateArray().Any(part => part.GetProperty("key").GetString() == "Projects")).Select(assignment => assignment.GetProperty("address")).ToArray();
        arguments["retiredEventAddresses"] = Page("eventContracts", revision).EnumerateArray().Select(assignment => assignment.GetProperty("address")).ToArray();
        var proposal = Result("propose-source", arguments);
        proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_enforce_safe_references_and_allow_an_explicit_draft()
    {
        Initialize();
        var opened = Open();
        var arguments = (Dictionary<string, object?>)Arguments(opened, Replace(opened, Source.Replace("        name ProjectName", "        name ProjectName\n        notes UnknownProjectName", StringComparison.Ordinal)));
        Failure(arguments).TryGetProperty("proposalId", out _).ShouldBeFalse();
        arguments["referencePolicy"] = "Draft";
        Result("propose-source", arguments).GetProperty("referencePolicy").GetString().ShouldEqual("Draft");
    }

    [Fact]
    void should_reject_typed_nodes_and_unknown_document_fields()
    {
        Initialize();
        var opened = Open();
        foreach (var document in new object[]
        {
            new { operation = "create-document", path = "bad.play", stableKey = "bad", node = new { kind = "ApplicationSyntax" } },
            new { operation = "create-document", path = "bad.play", stableKey = "bad", source = Streams, unexpected = true },
            new { operation = "create-document", path = "bad.play", stableKey = "bad", source = 42 }
        })
        {
            Call("propose-source", Arguments(opened, document)).GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
        }
    }

    object Replace(JsonElement opened, string source) => new { operation = "replace-document", documentId = Page("documents", opened.GetProperty("revision").GetString())[0].GetProperty("documentId").GetString(), source };

    JsonElement Failure(object arguments)
    {
        var result = Call("propose-source", arguments).GetProperty("result");
        result.GetProperty("isError").GetBoolean().ShouldBeTrue();
        return result.GetProperty("structuredContent");
    }

    static object Arguments(JsonElement opened, object document, string? validation = null) => Arguments(opened, [document], validation);

    static object Arguments(JsonElement opened, object[] documents, string? validation = null)
    {
        var arguments = new Dictionary<string, object?>
        {
            ["expectedRevision"] = opened.GetProperty("revision").GetString(),
            ["expectedCatalogRevision"] = opened.GetProperty("catalogRevision").GetString(),
            ["formatting"] = "CanonicalizeTouchedDocuments",
            ["documents"] = documents
        };
        if (validation is not null) arguments["validation"] = validation;
        return arguments;
    }
}

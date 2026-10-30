// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reviewing_destination_intent : given.a_destination_repair
{
    [Fact]
    void should_keep_intent_actions_separate_from_no_input_repairs()
    {
        Initialize();
        var capabilities = Result("repair-capabilities");
        capabilities.GetProperty("actions").GetArrayLength().ShouldEqual(15);
        capabilities.GetProperty("actions").EnumerateArray().Any(action => action.GetProperty("diagnosticCode").GetString() == "PLAY0470").ShouldBeFalse();
        var intent = capabilities.GetProperty("intentActions").EnumerateArray().Single();
        intent.GetProperty("diagnosticCode").GetString().ShouldEqual("PLAY0470");
        intent.GetProperty("canFixAll").GetBoolean().ShouldBeFalse();
        intent.GetProperty("pinRepairEvidence").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    void should_retain_paged_routing_without_writing_and_apply_exact_reviewed_bytes()
    {
        Start();
        var proposal = ProposeDestination();
        ShouldHaveNoInstalledSource();
        proposal.GetProperty("validation").GetString().ShouldEqual("Executable");
        proposal.GetProperty("repairEvidence").ValueKind.ShouldEqual(System.Text.Json.JsonValueKind.Null);
        var rows = new List<string>();
        for (var offset = 0; offset < 3; offset++)
        {
            var result = Review(proposal, offset: offset).GetProperty("result");
            result.GetProperty("summary").GetProperty("baselineExecutableAvailable").GetBoolean().ShouldBeFalse();
            result.GetProperty("summary").GetProperty("preservationProven").GetBoolean().ShouldBeFalse();
            result.GetProperty("summary").GetProperty("bindingSucceeded").GetBoolean().ShouldBeTrue();
            rows.Add(result.GetProperty("page").GetProperty("items")[0].GetProperty("classification").GetString()!);
        }
        rows.ShouldEqual(new[] { "MadeExplicit", "MadeExplicit", "ExplicitUnchanged" });
        var changes = Content(Workspaces.ReadProposal(Arguments(new { proposalId = proposal.GetProperty("proposalId").GetString() })));
        var document = changes.GetProperty("result").GetProperty("items")[0].GetProperty("documentId").GetString();
        var after = Content(Workspaces.ReadProposal(Arguments(new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "after", documentId = document })));
        var bytes = after.GetProperty("result").GetProperty("content").GetProperty("bytesBase64").GetBytesFromBase64();
        ShouldHaveNoInstalledSource();
        Install(proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(bytes);
        File.ReadAllText(Path.Combine(RootPath, "Handler.cs")).ShouldEqual("opaque implementation");
        File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldBeTrue();
    }

    [Theory]
    [InlineData("version")]
    [InlineData("unknown")]
    [InlineData("null-expression")]
    [InlineData("legacy-with-handles")]
    [InlineData("named-without-handles")]
    void should_reject_malformed_intent_without_retaining_a_partial_candidate(string malformed)
    {
        Start();
        var request = DestinationRequest();
        var intent = request["destinationIntent"]!;
        var first = intent["productions"]![0]!;
        switch (malformed)
        {
            case "version": intent["version"] = 2; break;
            case "unknown": intent["guess"] = true; break;
            case "null-expression": first["for"] = null; break;
            case "legacy-with-handles": first["route"]!["source"] = first["subject"]!.DeepClone(); break;
            case "named-without-handles": first["route"] = new JsonObject { ["kind"] = "Named" }; break;
        }
        Catch.Exception(() => Workspaces.ProposeRepair(Arguments(request))).ShouldNotBeNull();
        ShouldHaveNoInstalledSource();
        FailureOf(() => Workspaces.ReadProposal(Arguments(new { proposalId = "not-retained" }))).ShouldEqual("UnknownProposal");
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("duplicate")]
    [InlineData("explicit-retarget")]
    [InlineData("formatting")]
    void should_refuse_incomplete_or_retargeting_intent_as_one_transaction(string reason)
    {
        Start();
        var request = DestinationRequest();
        var productions = (JsonArray)request["destinationIntent"]!["productions"]!;
        switch (reason)
        {
            case "missing": productions.RemoveAt(1); break;
            case "duplicate": productions[1] = productions[0]!.DeepClone(); break;
            case "explicit-retarget": productions[2]!["for"]!["path"] = "name"; break;
            case "formatting": request["formatting"] = "PreserveExactSource"; break;
        }
        var refusal = Content(Workspaces.ProposeRepair(Arguments(request)));
        refusal.GetProperty("success").GetBoolean().ShouldBeFalse();
        refusal.TryGetProperty("proposalId", out _).ShouldBeFalse();
        ShouldHaveNoInstalledSource();
    }

    [Fact]
    void should_refuse_unrelated_workspace_binding_blockers_without_retaining_a_candidate()
    {
        Start(sourcePrefix: "system Outside\n");
        var refusal = Content(Workspaces.ProposeRepair(Arguments(DestinationRequest())));
        refusal.GetProperty("success").GetBoolean().ShouldBeFalse();
        refusal.GetProperty("failureKind").GetString().ShouldEqual("ProposalRejected");
        refusal.TryGetProperty("proposalId", out _).ShouldBeFalse();
        refusal.GetProperty("conflicts").EnumerateArray().Single().GetProperty("kind").GetString().ShouldEqual("CompilationFailed");
        refusal.GetProperty("executableReady").GetBoolean().ShouldBeFalse();
        var diagnostic = refusal.GetProperty("executableDiagnostics").EnumerateArray().Single(diagnostic => diagnostic.GetProperty("code").GetString() == "PLAY0268");
        diagnostic.GetProperty("severity").GetString().ShouldEqual("Error");
        diagnostic.GetProperty("arguments").GetProperty("issue").GetProperty("value").GetString().ShouldEqual("Cratis/Screenplay#301");
        diagnostic.GetProperty("location").GetProperty("path").GetString().ShouldEqual("application.play");
        diagnostic.GetProperty("location").GetProperty("line").GetInt32().ShouldEqual(1);
        ShouldHaveNoInstalledSource();
    }

    [Fact]
    void should_reject_pinned_evidence_for_the_intent_contract()
    {
        Start();
        var request = DestinationRequest();
        request["pinRepairEvidence"] = true;
        request["expectedRepairEvidenceRevision"] = McpRepairEvidence.Revision(McpAttachmentContents.Refresh(Root, Workspace()));
        FailureOf(() => Workspaces.ProposeRepair(Arguments(request))).ShouldEqual("UnsupportedRepair");
        ShouldHaveNoInstalledSource();
    }
}

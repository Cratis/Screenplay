// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_selecting_retained_stream_routes : given.an_authoring_connection
{
    const string Source = "import Account.Transactions\ntype Transactions\n  value String\neventsource Account\n  id \"StoredAccount\"\n  stream Transactions\n    id \"StoredTransactions\"\n  stream Other\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        value String\n        stream Account.Transactions\n";

    [Theory]
    [InlineData("wrong-route")]
    [InlineData("removed-reference")]
    [InlineData("changed-declaration")]
    [InlineData("preserve-trivia")]
    [InlineData("missing-import-removal")]
    void should_refuse_changes_not_proven_by_exact_candidate_correspondence(string change)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var opened = Open();
        var command = Node("CommandSyntax", opened.GetProperty("revision").GetString());
        var node = Selected(command);
        var operations = new List<object> { new { operation = "replace", target = command.GetProperty("handle"), node } };
        if (change != "missing-import-removal") operations.Add(new { operation = "remove", target = Node("ImportSyntax", opened.GetProperty("revision").GetString()).GetProperty("handle") });
        if (change == "wrong-route") node["stream"]!["stream"] = "Other";
        if (change == "removed-reference") node["properties"] = new JsonArray();
        if (change == "changed-declaration")
        {
            var declaration = Node("EventSourceSyntax", opened.GetProperty("revision").GetString());
            var source = JsonNode.Parse(declaration.GetProperty("node").GetRawText())!;
            source["id"] = "UnrelatedIdentity";
            operations.Add(new { operation = "replace", target = declaration.GetProperty("handle"), node = source });
        }
        var response = Call("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring", referencePolicy = "Draft", formatting = change == "preserve-trivia" ? "PreserveTrivia" : "CanonicalizeTouchedDocuments", operations
        });
        response.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        response.GetRawText().ShouldContain("ProposalRejected");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    }

    [Fact]
    void should_keep_catalog_assignments_and_pins_and_require_fresh_acceptance_after_preview()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        var opened = Open();
        var command = Node("CommandSyntax", opened.GetProperty("revision").GetString());
        var before = ScreenplayWorkspaceSerializer.Deserialize(Result("export-workspace", new { expectedRevision = opened.GetProperty("revision").GetString() }).GetProperty("bytesBase64").GetBytesFromBase64());
        var proposal = Result("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            validation = "Authoring", formatting = "CanonicalizeTouchedDocuments",
            operations = new object[] { new { operation = "replace", target = command.GetProperty("handle"), node = Selected(command) }, new { operation = "remove", target = Node("ImportSyntax", opened.GetProperty("revision").GetString()).GetProperty("handle") } }
        });
        var candidate = Candidate(proposal);
        candidate.IdentityCatalog.Application.ShouldEqual(before.IdentityCatalog.Application);
        candidate.Documents.Single().Id.ShouldEqual(before.Documents.Single().Id);
        candidate.IdentityCatalog.Semantics.ShouldEqual(before.IdentityCatalog.Semantics);
        candidate.IdentityCatalog.EventContracts.ShouldEqual(before.IdentityCatalog.EventContracts);
        candidate.Documents.Single().Text.ShouldContain("StoredAccount");
        candidate.Documents.Single().Text.ShouldContain("StoredTransactions");
        candidate.Compilation.Success.ShouldBeFalse();
        candidate.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268").ShouldBeTrue();
        var stale = Call("apply", new
        {
            expectedRevision = candidate.Revision.ToString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            proposalId = proposal.GetProperty("proposalId").GetString()
        });
        stale.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        stale.GetRawText().ShouldContain("StaleRevision");
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source + "// new revision\n");
        var refused = Call("apply", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(), expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            proposalId = proposal.GetProperty("proposalId").GetString()
        });
        refused.GetProperty("result").GetProperty("isError").GetBoolean().ShouldBeTrue();
        refused.GetRawText().ShouldContain("DiskDrift");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("import Account.Transactions");
    }

    static JsonNode Selected(JsonElement command)
    {
        var node = JsonNode.Parse(command.GetProperty("node").GetRawText())!;
        var selected = node["streamCandidates"]![0]!.DeepClone();
        selected["propertyCandidate"] = null;
        node["stream"] = selected;
        node["streamCandidates"] = new JsonArray();

        return node;
    }
}

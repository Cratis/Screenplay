// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_proposing_a_property_removal : given.a_property_generation_connection
{
    JsonElement _proposal;
    JsonElement _review;
    JsonElement _applied;
    ScreenplayWorkspace _before;
    ScreenplayWorkspace _after;
    byte[] _reviewed;

    void Because()
    {
        var opened = Open();
        _before = Workspace();
        var revision = opened.GetProperty("revision").GetString();
        var target = Result("read-ast", new { expectedRevision = revision, kind = "PropertySyntax", name = "name" }).GetProperty("page").GetProperty("items").EnumerateArray()
            .Single(node => node.GetProperty("address").GetProperty("parts").EnumerateArray().Any(part => part.GetProperty("kind").GetString() == "Generation" && part.GetProperty("key").GetString() == "2")).GetProperty("handle");
        _proposal = Result("propose-property", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            action = "remove", target, expectedName = "name", newGeneration = true,
            formatting = "CanonicalizeTouchedDocuments"
        });
        _after = Candidate(_proposal);
        var id = _proposal.GetProperty("proposalId").GetString();
        _review = Result("read-proposal", new { proposalId = id, view = "semantic-diff" });
        _reviewed = Result("read-proposal", new { proposalId = id, view = "after", documentId = target.GetProperty("documentId").GetString() }).GetProperty("result").GetProperty("content").GetProperty("bytesBase64").GetBytesFromBase64();
        _applied = Apply(opened, _proposal);
    }

    [Fact] void should_match_the_remove_result_json_snapshot() => given.property_result_snapshots.Verify("remove-event", _proposal.GetProperty("property"));
    [Fact] void should_keep_the_plan_in_retained_semantic_review() => _review.GetProperty("property").GetRawText().ShouldEqual(_proposal.GetProperty("property").GetRawText());
    [Fact] void should_apply_the_complete_plan() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_install_the_exact_reviewed_bytes() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(_reviewed);
    [Fact] void should_keep_the_historical_identity() => Property(_after, "Audited", "name", 2).SemanticId.ShouldEqual(Property(_before, "Audited", "name", 2).SemanticId);
    [Fact] void should_remove_only_the_selected_current_property() => ((EventSyntax)WorkspaceSyntaxIndex.Create(_after).Entries.Single(entry => entry.Node is EventSyntax @event && @event.Name == "Audited" && @event.Generation == 3).Node).Properties.ShouldBeEmpty();
    [Fact] void should_leave_the_connected_command_input() => _after.Documents.Single().Text.Contains("name String", StringComparison.Ordinal).ShouldBeTrue();
}

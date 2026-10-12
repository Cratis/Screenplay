// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_proposing_a_property_add : given.an_authoring_connection
{
    JsonElement _proposal;
    JsonElement _review;
    JsonElement _diff;
    JsonElement _applied;
    JsonElement _reopened;
    ScreenplayWorkspace _candidate;
    bool _unchanged;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var owner = Node("CommandSyntax", revision).GetProperty("handle");
        var @event = Node("EventSyntax", revision).GetProperty("handle");
        var production = Node("ProducesSyntax", revision).GetProperty("handle");
        var before = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        _proposal = Result("propose-property", new
        {
            expectedRevision = revision, expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(), action = "add", owner, expectedOwnerName = "RegisterProject", name = "priority", type = "String",
            hops = new object[] { new { at = @event, name = "priority" }, new { at = production, value = "priority" } }, eventNeverPersisted = true, formatting = "CanonicalizeTouchedDocuments", validation = "Executable"
        });
        _candidate = Candidate(_proposal);
        var id = _proposal.GetProperty("proposalId").GetString();
        _review = Result("read-proposal", new { proposalId = id });
        _diff = Result("read-proposal", new { proposalId = id, view = "semantic-diff" });
        _unchanged = File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(before);
        _applied = Apply(opened, _proposal);
        _reopened = Open();
    }

    [Fact] void should_not_write_on_propose() => _unchanged.ShouldBeTrue();
    [Fact] void should_retain_the_same_report() => _review.GetProperty("property").GetRawText().ShouldEqual(_proposal.GetProperty("property").GetRawText());
    [Fact] void should_disclose_property_added_in_the_semantic_diff() => _diff.GetRawText().ShouldContain("property-added");
    [Fact] void should_apply_the_reviewed_candidate() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_reopen_at_the_candidate_revision() => _reopened.GetProperty("revision").GetString().ShouldEqual(_proposal.GetProperty("after").GetProperty("revision").GetString());
    [Fact] void should_have_two_added_properties() => WorkspaceSyntaxIndex.Create(_candidate).Entries.Count(entry => entry.Node is PropertySyntax property && property.Name == "priority").ShouldEqual(2);
    [Fact] void should_report_one_in_place_event_edit() => _proposal.GetProperty("property").GetProperty("generations")[0].GetProperty("mode").GetString().ShouldEqual("eventNeverPersisted");
    [Fact] void should_not_publish_the_singular_alias() => _proposal.GetProperty("property").TryGetProperty("generation", out _).ShouldBeFalse();
}

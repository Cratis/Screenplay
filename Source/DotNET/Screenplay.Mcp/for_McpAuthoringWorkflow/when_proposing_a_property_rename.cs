// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_proposing_a_property_rename : given.an_authoring_connection
{
    JsonElement _proposal;
    JsonElement _review;
    JsonElement _diff;
    JsonElement _applied;
    ScreenplayWorkspace _before;
    ScreenplayWorkspace _after;
    byte[] _reviewed;
    byte[] _expected;
    bool _unchanged;

    void Establish()
    {
        var original = "\uFEFF// keep this comment\r\n" + Source.Replace("\n", "\r\n", StringComparison.Ordinal);
        File.WriteAllBytes(Path.Combine(RootPath, "application.play"), Encoding.UTF8.GetBytes(original));
        _expected = Encoding.UTF8.GetBytes(original.Replace("name ProjectName", "label ProjectName", StringComparison.Ordinal).Replace("name = name", "name = label", StringComparison.Ordinal)
            .Replace("event ProjectRegistered\r\n        projectId ProjectId\r\n        label ProjectName", "event ProjectRegistered\r\n        projectId ProjectId\r\n        name ProjectName", StringComparison.Ordinal));
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        _before = Workspace();
        var target = Result("read-ast", new { expectedRevision = revision, kind = "PropertySyntax", name = "name" }).GetProperty("page").GetProperty("items").EnumerateArray().Single(node => node.GetProperty("handle").GetProperty("path").GetString().Contains("/commands/", StringComparison.Ordinal)).GetProperty("handle");
        _proposal = Result("propose-property", new { expectedRevision = revision, expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(), action = "rename", target, expectedName = "name", newName = "label" });
        _after = Candidate(_proposal);
        var id = _proposal.GetProperty("proposalId").GetString();
        _review = Result("read-proposal", new { proposalId = id });
        _diff = Result("read-proposal", new { proposalId = id, view = "semantic-diff" });
        _reviewed = Result("read-proposal", new { proposalId = id, view = "after", documentId = target.GetProperty("documentId").GetString() }).GetProperty("result").GetProperty("content").GetProperty("bytesBase64").GetBytesFromBase64();
        _unchanged = !File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(_expected);
        _applied = Apply(opened, _proposal);
    }

    [Fact] void should_leave_disk_untouched_until_apply() => _unchanged.ShouldBeTrue();
    [Fact] void should_review_the_exact_trivia_preserving_source() => _reviewed.ShouldEqual(_expected);
    [Fact] void should_install_only_the_reviewed_bytes() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).ShouldEqual(_reviewed);
    [Fact] void should_apply_successfully() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_the_property_report_in_retained_review() => _review.GetProperty("property").GetRawText().ShouldEqual(_proposal.GetProperty("property").GetRawText());
    [Fact] void should_keep_the_property_report_in_semantic_diff_review() => _diff.GetProperty("property").GetRawText().ShouldEqual(_proposal.GetProperty("property").GetRawText());
    [Fact] void should_return_only_the_required_generations_array() => _proposal.GetProperty("property").GetProperty("generations").GetArrayLength().ShouldEqual(0);
    [Fact] void should_not_publish_a_singular_generation_alias() => _proposal.GetProperty("property").TryGetProperty("generation", out _).ShouldBeFalse();
    [Fact] void should_keep_command_property_identity() => WorkspaceSyntaxIndex.Create(_after).Entries.Single(entry => entry.Node is PropertySyntax property && property.Name == "label").SemanticId.ShouldEqual(WorkspaceSyntaxIndex.Create(_before).Entries.Single(entry => entry.Node is PropertySyntax property && property.Name == "name" && entry.Handle.Path.Contains("/commands/", StringComparison.Ordinal)).SemanticId);
    [Fact] void should_keep_unselected_event_fields() => ((EventSyntax)WorkspaceSyntaxIndex.Create(_after).Entries.Single(entry => entry.Node is EventSyntax).Node).Properties.Any(property => property.Name == "name").ShouldBeTrue();
    [Fact] void should_match_the_result_json_snapshot() => given.property_result_snapshots.Verify("zero-events", _proposal.GetProperty("property"));
}

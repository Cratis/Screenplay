// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reading_ambiguous_handler_intents : given.a_connection
{
    const string Duplicate = "module M\n  feature F\n    slice StateChange S\n      command C\n        handler\n          implementation\n            hint \"Keep\"";
    JsonElement _page;
    JsonElement _details;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Duplicate);
        File.WriteAllText(Path.Combine(RootPath, "duplicate.play"), Duplicate);
        Initialize();
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        _page = Call("read-workspace", new { expectedRevision = revision, view = "handler-intents" }).GetProperty("result").GetProperty("structuredContent");
        var id = _page.GetProperty("page").GetProperty("items")[0].GetProperty("requirementId").GetString();
        _details = Call("read-workspace", new { expectedRevision = revision, view = "handler-intent-details", requirementId = id }).GetProperty("result");
    }

    [Fact] void should_report_both_occurrences_without_inventing_requirement_identity()
    {
        var items = _page.GetProperty("page").GetProperty("items").EnumerateArray().ToArray();
        items.Length.ShouldEqual(2);
        items[0].GetProperty("requirementId").GetString().ShouldEqual(items[1].GetProperty("requirementId").GetString());
        items[0].GetProperty("handle").GetProperty("documentId").GetString().ShouldNotEqual(items[1].GetProperty("handle").GetProperty("documentId").GetString());
        items.All(item => item.GetProperty("ambiguous").GetBoolean()).ShouldBeTrue();
        Workspace().Compilation.Success.ShouldBeFalse();
    }

    [Fact] void should_refuse_details_as_an_actionable_tool_conflict_not_an_internal_exception()
    {
        _details.GetProperty("isError").GetBoolean().ShouldBeTrue();
        var text = _details.GetProperty("content")[0].GetProperty("text").GetString()!;
        text.Contains("AmbiguousRequirement:", StringComparison.Ordinal).ShouldBeTrue();
        text.Contains("repair duplicate declarations", StringComparison.Ordinal).ShouldBeTrue();
        text.Contains("Internal", StringComparison.Ordinal).ShouldBeFalse();
        text.Contains("Sequence contains", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact] void should_cache_model_only_inventory_per_immutable_snapshot()
    {
        var workspace = Workspace();
        var analysis = McpWorkspaceAnalysis.For(workspace);
        ReferenceEquals(analysis.HandlerIntents, analysis.HandlerIntents).ShouldBeTrue();
        ReferenceEquals(analysis, McpWorkspaceAnalysis.For(workspace)).ShouldBeTrue();
        var restarted = ScreenplayWorkspaceSerializer.Deserialize(ScreenplayWorkspaceSerializer.Serialize(workspace));
        ReferenceEquals(analysis.HandlerIntents, McpWorkspaceAnalysis.For(restarted).HandlerIntents).ShouldBeFalse();
    }
}

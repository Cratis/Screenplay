// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reviewing_semantic_diff_without_apps : given.a_connection
{
    JsonElement _diff;
    JsonElement _stale;
    JsonElement _repeat;
    JsonElement _diskChanged;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var proposal = Call("expand-layout", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            layout = "slice"
        }).GetProperty("result").GetProperty("structuredContent");
        var id = proposal.GetProperty("proposalId").GetString();
        _diff = Call("read-proposal", new { proposalId = id, view = "semantic-diff", limit = 1 }).GetProperty("result");
        _repeat = Call("read-proposal", new { proposalId = id, view = "semantic-diff", limit = 1 }).GetProperty("result");
        _stale = Call("read-proposal", new { proposalId = id, view = "semantic-diff", offset = 1, expectedSourceRevision = "stale" }).GetProperty("result");
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// changed on disk");
        _diskChanged = Call("read-proposal", new { proposalId = id, view = "semantic-diff" }).GetProperty("result");
    }

    [Fact] void should_offer_the_view_without_mcp_apps() => _diff.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_no_semantic_change_for_expand_layout() => _diff.GetProperty("structuredContent").GetProperty("result").GetProperty("hasSemanticChange").GetBoolean().ShouldBeFalse();
    [Fact] void should_return_identical_wire_bytes() => _repeat.GetRawText().ShouldEqual(_diff.GetRawText());
    [Fact] void should_refuse_a_stale_revision_through_the_protocol() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_refuse_a_changed_disk_baseline() => _diskChanged.GetProperty("isError").GetBoolean().ShouldBeTrue();
}

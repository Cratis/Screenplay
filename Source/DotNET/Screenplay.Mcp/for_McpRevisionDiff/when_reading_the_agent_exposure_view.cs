// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_reading_the_agent_exposure_view : given.two_snapshots
{
    JsonElement _result;
    JsonElement _traversal;
    JsonElement _stale;

    void Because()
    {
        _result = Call(new { beforeWorkspaceJson = BeforeJson, afterWorkspaceJson = AfterJson, view = "agent-exposure", version = 2, limit = 1 });
        var source = JsonNode.Parse(BeforeJson)!;
        source["documents"]![0]!["path"] = "../outside.play";
        _traversal = Call(new { beforeWorkspaceJson = source.ToJsonString(), afterWorkspaceJson = AfterJson, view = "agent-exposure", version = 2 });
        _stale = Call(new { beforeWorkspaceJson = BeforeJson, afterWorkspaceJson = AfterJson, view = "agent-exposure", version = 2, offset = 1, expectedSourceRevision = "stale" });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    void should_refuse_a_different_answer_version(int version) => Catch.Exception(() => Call(new { beforeWorkspaceJson = BeforeJson, afterWorkspaceJson = AfterJson, view = "agent-exposure", version }))!.Message.ShouldContain("'version' must equal 2");

    [Fact] void should_answer_the_requested_version() => _result.GetProperty("structuredContent").GetProperty("version").GetInt32().ShouldEqual(2);
    [Fact] void should_pin_even_an_empty_page() => _result.GetProperty("structuredContent").GetProperty("page").GetProperty("revision").GetString().ShouldEqual(_result.GetProperty("structuredContent").GetProperty("sourceRevision").GetString());
    [Fact] void should_support_the_existing_revision_tool() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_identify_the_opted_in_view() => _result.GetProperty("structuredContent").GetProperty("comparisonLevel").GetString().ShouldEqual("agent-exposure");
    [Fact] void should_compare_an_empty_allowlist_completely() => _result.GetProperty("structuredContent").GetProperty("coverage").GetString().ShouldEqual("Complete");
    [Fact] void should_refuse_paths_outside_the_snapshot_root() => _traversal.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("UnreadableRevision");
    [Fact] void should_keep_revision_checks_on_the_new_view() => _stale.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("StaleRevision");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_discovering_the_snapshot_contract : Specification
{
    JsonElement _tool;

    void Because() => _tool = JsonSerializer.SerializeToElement(McpToolCatalog.Describe(), McpJson.Options).EnumerateArray().Single(tool => tool.GetProperty("name").GetString() == "semantic-diff");

    [Fact] void should_require_both_snapshot_strings() => _tool.GetProperty("inputSchema").GetProperty("required").EnumerateArray().Select(value => value.GetString()).ShouldContainOnly("beforeWorkspaceJson", "afterWorkspaceJson");
    [Fact] void should_advertise_a_read_only_tool() => _tool.GetProperty("annotations").GetProperty("readOnlyHint").GetBoolean().ShouldBeTrue();
    [Fact] void should_bound_the_page_count() => _tool.GetProperty("inputSchema").GetProperty("properties").GetProperty("limit").GetProperty("maximum").GetInt32().ShouldEqual(200);
    [Fact] void should_disclose_that_refs_are_not_snapshot_inputs() => _tool.GetProperty("inputSchema").GetProperty("properties").GetProperty("beforeWorkspaceJson").GetProperty("description").GetString()!.Contains("not a Git ref or path", StringComparison.Ordinal).ShouldBeTrue();
}

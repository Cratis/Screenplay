// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLargeModels;

public class when_materializing_a_response_once : Specification
{
    int _visited;
    string _response = string.Empty;

    void Because() => _response = JsonSerializer.Serialize(McpJson.ToolResult(new { items = Items() }), McpJson.Options);

    [Fact] void should_materialize_each_item_once_for_both_wire_representations() => _visited.ShouldEqual(3);
    [Fact] void should_emit_the_structured_representation() => _response.Contains("structuredContent", StringComparison.Ordinal).ShouldBeTrue();

    IEnumerable<int> Items()
    {
        for (var index = 0; index < 3; index++)
        {
            _visited++;
            yield return index;
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_levels_are_module : given.a_graph_query
{
    JsonElement _item;

    void Because()
    {
        _result = Read(_snapshot, new { evidenceLimit = 1 });
        _item = _result.GetProperty("page").GetProperty("items").EnumerateArray().Single();
    }

    [Fact] void should_use_module_nodes() => _item.GetProperty("source").GetProperty("kind").GetString().ShouldEqual("Module");
    [Fact] void should_keep_the_consumer_address() => _item.GetProperty("source").GetProperty("address").GetString().ShouldEqual("A");
    [Fact] void should_keep_the_producer_address() => _item.GetProperty("target").GetProperty("address").GetString().ShouldEqual("B");
    [Fact] void should_hide_test_only_references() => _item.GetProperty("references").GetInt32().ShouldEqual(1);
    [Fact] void should_use_authored_ranks() => _result.GetProperty("orderSource").GetString().ShouldEqual("authored");
}

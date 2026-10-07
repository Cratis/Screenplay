// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_the_tool_requires_revision_pinning : for_McpConnection.given.a_connection
{
    JsonElement _first;
    JsonElement _next;
    JsonElement _missing;
    JsonElement _stale;

    void Establish() => Initialize();

    void Because()
    {
        _first = Call("dependency-graph", new { view = "order", limit = 1 }).GetProperty("result").GetProperty("structuredContent");
        var expectedSourceRevision = _first.GetProperty("sourceRevision").GetString();
        _missing = Call("dependency-graph", new { view = "order", limit = 1, offset = 1 });
        _next = Call("dependency-graph", new { view = "order", limit = 1, offset = 1, expectedSourceRevision }).GetProperty("result").GetProperty("structuredContent");
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// changed source\n");
        _stale = Call("dependency-graph", new { view = "order", limit = 1, offset = 1, expectedSourceRevision }).GetProperty("result");
    }

    [Fact] void should_return_the_next_pinned_page() => _next.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
    [Fact] void should_require_a_revision_on_continuation() => _missing.GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_refuse_stale_source() => _stale.GetProperty("isError").GetBoolean().ShouldBeTrue();
}

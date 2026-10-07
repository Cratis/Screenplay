// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_names_are_duplicated : given.a_graph_query
{
    JsonElement[] _views;

    void Establish() => _snapshot = Snapshot("module M\n  feature F\n    slice StateView Duplicate\n      projection P\n        from E\n    slice StateView Duplicate\n      projection Q\n        from Missing\n  feature F\n    slice StateChange Producer\n      event E\nmodule M\n  feature G\n    slice StateView Other\n      projection R\n        from E\n");
    void Because() => _views = [.. new[] { "edges", "cycles", "order", "unresolved" }.Select(view => Read(_snapshot, new { view, from = "slice", to = "slice" }))];

    [Fact] void should_answer_edges() => _views[0].GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(2);
    [Fact] void should_answer_cycles() => _views[1].GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(0);
    [Fact] void should_answer_order() => _views[2].GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(4);
    [Fact] void should_answer_unresolved() => _views[3].GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(1);
}

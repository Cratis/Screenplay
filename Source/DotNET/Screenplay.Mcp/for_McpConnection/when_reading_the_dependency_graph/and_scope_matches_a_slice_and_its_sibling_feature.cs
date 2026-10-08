// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_scope_matches_a_slice_and_its_sibling_feature : for_McpConnection.given.a_connection
{
    readonly Dictionary<string, JsonElement> _responses = new(StringComparer.Ordinal);

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module M\n  feature F\n    slice StateView Sub\n      projection P\n        from E\n    feature Sub\n      slice StateChange Producer\n        event E\n");
        Initialize();
    }

    void Because()
    {
        foreach (var view in new[] { "edges", "cycles", "order", "unresolved" })
        {
            _responses[view] = Call("dependency-graph", new { view, from = "slice", to = "slice", scope = "M.F.Sub" });
        }
    }

    [Fact] void should_reject_ambiguous_edge_scope() => _responses["edges"].GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_reject_ambiguous_cycle_scope() => _responses["cycles"].GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_reject_ambiguous_order_scope() => _responses["order"].GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_reject_ambiguous_unresolved_scope() => _responses["unresolved"].GetProperty("error").GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_explain_the_ambiguity() => _responses["edges"].GetProperty("error").GetProperty("message").GetString().ShouldEqual("Dependency scope 'M.F.Sub' is ambiguous; it matches multiple node kinds.");
}

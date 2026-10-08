// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_scope_names_a_unique_node : given.a_graph_query
{
    JsonElement _outgoing;
    JsonElement _incoming;
    JsonElement _unrelated;
    JsonElement _order;
    JsonElement _unresolved;
    Exception _missing;

    void Establish() => _snapshot = Snapshot(Source + "module C\n  feature H\n    feature Nested\n      slice StateView Missing\n        projection Q\n          from Unknown\n");

    void Because()
    {
        _outgoing = Read(_snapshot, new { from = "slice", to = "slice", scope = "A.F" }).GetProperty("page").GetProperty("items");
        _incoming = Read(_snapshot, new { from = "slice", to = "slice", scope = "B.G.W", direction = "incoming" }).GetProperty("page").GetProperty("items");
        _unrelated = Read(_snapshot, new { from = "slice", to = "slice", scope = "B.G.W" }).GetProperty("page").GetProperty("items");
        _order = Read(_snapshot, new { view = "order", scope = "C.H" }).GetProperty("page").GetProperty("items");
        _unresolved = Read(_snapshot, new { view = "unresolved", scope = "C.H" }).GetProperty("page").GetProperty("items");
        _missing = Catch.Exception(() => Read(_snapshot, new { scope = "Missing" }));
    }

    [Fact] void should_include_descendant_consumers() => _outgoing.EnumerateArray().Single().GetProperty("source").GetProperty("address").GetString().ShouldEqual("A.F.V");
    [Fact] void should_filter_incoming_edges_by_the_producer() => _incoming.EnumerateArray().Single().GetProperty("target").GetProperty("address").GetString().ShouldEqual("B.G.W");
    [Fact] void should_exclude_edges_outside_the_consuming_scope() => _unrelated.GetArrayLength().ShouldEqual(0);
    [Fact] void should_include_only_the_scoped_order_containers() => _order.EnumerateArray().Select(item => item.GetProperty("container").GetProperty("address").GetString()).ShouldEqual(["C.H", "C.H.Nested"]);
    [Fact] void should_include_unresolved_references_in_descendants() => _unresolved.EnumerateArray().Single().GetProperty("consumer").GetProperty("address").GetString().ShouldEqual("C.H.Nested.Missing");
    [Fact] void should_reject_a_missing_scope() => _missing.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_report_invalid_arguments_for_a_missing_scope() => ((McpFailure)_missing).Code.ShouldEqual(-32602);
}

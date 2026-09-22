// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp.for_McpConnection;

public class when_querying_scoped_fixture_values : given.a_connection
{
    JsonElement _first;
    JsonElement _second;
    JsonElement _filtered;

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), """
        concept ViewKind : Enum
          shown
        concept QueryKind : Enum
          requested
        module Records
          feature Entries
            slice StateView Browse
              readmodel View
                kind ViewKind
              query Lookup => View
                filter kind QueryKind
              specification LookingUp
                given readmodel View
                  kind = shown
                then readmodel View
                  kind = shown
                then query Lookup
                  arguments
                    kind = requested
                  result
                    kind = shown
        """);

    void Because()
    {
        var snapshot = new McpSnapshot(Root.Read());
        _first = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(snapshot, null, null, "kind", null, 0, 2), McpJson.Options);
        _second = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(snapshot, null, null, "kind", null, 2, 2), McpJson.Options);
        _filtered = JsonSerializer.SerializeToElement(McpFixtureQueries.Values(snapshot, "Records.Entries.Browse.LookingUp", "queryArguments", "kind", "requested"), McpJson.Options);
    }

    [Fact] void should_count_all_occurrences() => _first.GetProperty("page").GetProperty("total").GetInt32().ShouldEqual(4);
    [Fact] void should_count_matching_occurrences() => _first.GetProperty("page").GetProperty("matched").GetInt32().ShouldEqual(4);
    [Fact] void should_bound_the_page() => _first.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(2);
    [Fact] void should_report_truncation() => _first.GetProperty("page").GetProperty("truncated").GetBoolean().ShouldBeTrue();
    [Fact] void should_return_the_next_offset() => _first.GetProperty("page").GetProperty("nextOffset").GetInt32().ShouldEqual(2);
    [Fact] void should_finish_on_the_second_page() => _second.GetProperty("page").GetProperty("nextOffset").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_resolve_the_query_argument_type_in_its_own_shape() => _second.GetProperty("page").GetProperty("items")[0].GetProperty("declaredType").GetProperty("name").GetString().ShouldEqual("QueryKind");
    [Fact] void should_resolve_the_query_result_type_in_its_own_shape() => _second.GetProperty("page").GetProperty("items")[1].GetProperty("declaredType").GetProperty("name").GetString().ShouldEqual("ViewKind");
    [Fact] void should_filter_by_owner_role_property_and_value() => _filtered.GetProperty("page").GetProperty("matched").GetInt32().ShouldEqual(1);
    [Fact] void should_preserve_the_specification_owner() => _filtered.GetProperty("page").GetProperty("items")[0].GetProperty("specification").GetProperty("address").GetString().ShouldEqual("Records.Entries.Browse.LookingUp");
}

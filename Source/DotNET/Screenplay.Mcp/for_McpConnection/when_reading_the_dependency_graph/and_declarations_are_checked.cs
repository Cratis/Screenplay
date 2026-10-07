// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_reading_the_dependency_graph;

public class and_declarations_are_checked : given.a_graph_query
{
    static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web);
    JsonElement _item;
    JsonElement _details;

    void Because()
    {
        _snapshot = Snapshot(Source.Replace("module A", "module A\n  depends on B", StringComparison.Ordinal));
        _result = Read(_snapshot, new { view = "declarations" });
        _item = _result.GetProperty("page").GetProperty("items").EnumerateArray().Single();
        var arguments = JsonSerializer.SerializeToElement(new { address = "A", kind = "Module", view = "dependencies" });
        _details = JsonSerializer.SerializeToElement(McpDeclarationDetails.Read(_snapshot, arguments), _options);
    }

    [Fact] void should_page_only_opted_in_containers() => _item.GetProperty("container").GetProperty("address").GetString().ShouldEqual("A");
    [Fact] void should_resolve_the_authored_target() => _item.GetProperty("declarations")[0].GetProperty("resolved").GetString().ShouldEqual("B");
    [Fact] void should_mark_the_declaration_used() => _item.GetProperty("declarations")[0].GetProperty("status").GetString().ShouldEqual("used");
    [Fact] void should_exclude_specification_evidence() => _item.GetProperty("edges").GetArrayLength().ShouldEqual(1);
    [Fact] void should_show_the_covering_declaration() => _item.GetProperty("edges")[0].GetProperty("coveringDeclarations")[0].GetProperty("target").GetString().ShouldEqual("B");
    [Fact] void should_offer_the_authored_dependencies_view() => _details.GetProperty("details").GetProperty("items")[0].GetProperty("target").GetString().ShouldEqual("B");
}

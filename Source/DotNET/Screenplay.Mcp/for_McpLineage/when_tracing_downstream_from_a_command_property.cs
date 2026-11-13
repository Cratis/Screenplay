// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_tracing_downstream_from_a_command_property : given.a_lineage_model
{
    const string Title = "Lending.Catalog.AddBook.AddBook.title";

    JsonElement _report;
    JsonElement[] _hops = [];
    JsonElement _display;

    void Establish() => Write(File.ReadAllText(Path.Combine(Root(), "Samples", "Library", "library.play")));

    void Because()
    {
        _report = Call("trace-lineage", new { address = Title, direction = "downstream", limit = 200 }).GetProperty("result").GetProperty("structuredContent");
        _hops = Hops(_report);
        _display = _hops.Single(hop => hop.GetProperty("role").GetString() == "screen-display");
    }

    [Fact] void should_start_at_the_command_property() => Subject(_hops).GetProperty("role").GetString().ShouldEqual("command-property");
    [Fact] void should_report_the_direction() => _report.GetProperty("direction").GetString().ShouldEqual("downstream");
    [Fact] void should_follow_the_value_forward_to_the_screen_showing_it() => Chain(_hops, _display).ShouldEqual(["command-property", "production-mapping", "event-property", "projection-mapping", "read-model-property", "screen-display"]);
    [Fact] void should_name_every_member_on_the_way() => Chain(_hops, _display, "address").ShouldEqual([Title, "Lending.Catalog.AddBook.AddBook", "Lending.Catalog.AddBook.BookAdded.title", "Lending.Catalog.BookCatalog.BookCatalog", "Lending.Catalog.BookCatalog.CatalogEntry.title", "Lending.Catalog.BookCatalog.BookCatalog"]);
    [Fact] void should_list_the_queries_serving_the_read_model() => _hops.Count(hop => hop.GetProperty("role").GetString() == "query").ShouldEqual(2);
    [Fact] void should_not_go_upstream() => _hops.Any(hop => hop.GetProperty("direction").GetString() == "upstream").ShouldBeFalse();
    [Fact] void should_be_complete() => _report.GetProperty("complete").GetBoolean().ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_tracing_the_library_catalog : given.a_lineage_model
{
    const string Title = "Lending.Catalog.BookCatalog.CatalogEntry.title";

    JsonElement _report;
    JsonElement _again;
    JsonElement[] _hops = [];
    JsonElement _input;
    string[] _unresolved = [];

    void Establish() => Write(File.ReadAllText(Path.Combine(Root(), "Samples", "Library", "library.play")));

    void Because()
    {
        _report = Trace(Title);
        _again = Trace(Title);
        _hops = Hops(_report);
        _input = _hops.Single(hop => hop.GetProperty("role").GetString() == "screen-input");
        _unresolved = [.. _hops.Where(hop => Text(hop, "address") is not null && Call("declaration-details", new { address = Text(hop, "address"), kind = Text(hop, "kind") }).GetProperty("result").TryGetProperty("isError", out var error) && error.GetBoolean())
            .Select(hop => $"{Text(hop, "kind")} {Text(hop, "address")}")];
    }

    [Fact] void should_start_at_the_read_model_property() => Text(Subject(_hops), "address").ShouldEqual(Title);
    [Fact] void should_follow_the_mapping_chain_back_to_the_form_input() => Chain(_hops, _input).ShouldEqual(["read-model-property", "projection-mapping", "event-property", "production-mapping", "command-property", "screen-input"]);
    [Fact] void should_name_every_declaration_on_the_chain() => Chain(_hops, _input, "address").ShouldEqual([Title, "Lending.Catalog.BookCatalog.BookCatalog", "Lending.Catalog.AddBook.BookAdded.title", "Lending.Catalog.AddBook.AddBook", "Lending.Catalog.AddBook.AddBook.title", "Lending.AddBookInput"]);
    [Fact] void should_report_the_explicit_from_mapping() => _hops.Single(hop => hop.GetProperty("role").GetString() == "projection-mapping").GetProperty("mode").GetString().ShouldEqual("explicit");
    [Fact] void should_list_both_queries_serving_the_view() => _hops.Where(hop => hop.GetProperty("role").GetString() == "query").Select(hop => Text(hop, "address")).ShouldEqual(["Lending.Catalog.BookCatalog.AllBooks", "Lending.Catalog.BookCatalog.BookById"]);
    [Fact] void should_list_the_screen_column_showing_it() => _hops.Where(hop => hop.GetProperty("role").GetString() == "screen-display").Select(hop => Text(hop, "address")).ShouldContain("Lending.Catalog.BookCatalog.BookCatalog");
    [Fact] void should_resolve_every_address_through_declaration_details() => _unresolved.ShouldBeEmpty();
    [Fact] void should_be_complete() => _report.GetProperty("complete").GetBoolean().ShouldBeTrue();
    [Fact] void should_carry_a_source_location_on_every_hop() => _hops.All(hop => hop.GetProperty("location").ValueKind == JsonValueKind.Object).ShouldBeTrue();
    [Fact] void should_be_deterministic() => _again.GetRawText().ShouldEqual(_report.GetRawText());
}

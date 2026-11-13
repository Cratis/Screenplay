// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage;

public class when_tracing_from_a_screen_field : given.a_lineage_model
{
    const string Input = "Lending.AddBookInput.title";
    const string Column = "Lending.Catalog.BookCatalog.BookCatalog.title";

    JsonElement[] _input = [];
    JsonElement[] _column = [];

    void Establish() => Write(File.ReadAllText(Path.Combine(Root(), "Samples", "Library", "library.play")));

    void Because()
    {
        _input = Hops(Trace(Input));
        _column = Hops(Trace(Column));
    }

    [Fact] void should_start_at_the_form_field() => (Subject(_input).GetProperty("role").GetString(), Text(Subject(_input), "kind"), Text(Subject(_input), "address")).ShouldEqual(("screen-field", "Form", Input));
    [Fact] void should_follow_an_input_forward_to_the_screen_showing_it() => Chain(_input, _input.Single(hop => hop.GetProperty("role").GetString() == "screen-display")).ShouldEqual(["screen-field", "command-property", "production-mapping", "event-property", "projection-mapping", "read-model-property", "screen-display"]);
    [Fact] void should_have_no_upstream_for_an_input() => _input.Any(hop => hop.GetProperty("direction").GetString() == "upstream").ShouldBeFalse();
    [Fact] void should_start_at_the_screen_column() => (Subject(_column).GetProperty("role").GetString(), Text(Subject(_column), "kind")).ShouldEqual(("screen-field", "Screen"));
    [Fact] void should_follow_a_display_back_to_the_form_input() => Chain(_column, _column.Single(hop => hop.GetProperty("role").GetString() == "screen-input")).ShouldEqual(["screen-field", "read-model-property", "projection-mapping", "event-property", "production-mapping", "command-property", "screen-input"]);
    [Fact] void should_have_no_downstream_for_a_display() => _column.Any(hop => hop.GetProperty("direction").GetString() == "downstream").ShouldBeFalse();
}

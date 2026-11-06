// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticGraphRunner;

public class when_a_handler_iterates_a_collection : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Model = "model-1";
    const string First = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaa1";
    const string Second = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbb2";
    const string Owner = "11111111-1111-4111-8111-111111111111";

    const string Source =
        "command reads protected\n" +
        "concept SeriesId : Uuid\nconcept ModelId : Uuid\n" +
        "type Slot\n  slot String\n  series SeriesId\n" +
        "module M\n  feature F\n    slice StateView S\n" +
        "      event Defined\n        slots Slot[]\n        note String optional\n" +
        "      event Retired\n        slots Slot[]\n" +
        "      graph G\n        node Model\n          modelId ModelId key\n        node Series\n          seriesId SeriesId key\n" +
        "        edge SCORES source Model target Series identified by slot\n          slot String\n" +
        "      projection P => graph G\n" +
        "        node Series\n          from Defined foreach slots key slots.series\n" +
        "        edge SCORES\n" +
        "          from Defined foreach slots source $eventSourceId target slots.series identified by slots.slot\n            slot = slots.slot\n" +
        "          remove with Retired foreach slots source $eventSourceId target slots.series identified by slots.slot\n";

    static string Items(params (string Slot, string Series)[] items) =>
        "          slots\n" + string.Concat(items.Select(item => $"            item\n              slot = \"{item.Slot}\"\n              series = \"{item.Series}\"\n"));

    static string Defined(params (string Slot, string Series)[] items) => $"        given Defined\n          for \"{Owner}\"\n{Items(items)}";

    static string Edge(string series, string slot) => $"          has edge SCORES from \"{Owner}\" to \"{series}\" identified by \"{slot}\"\n";

    static string NoEdge(string series, string slot) => $"          no edge SCORES from \"{Owner}\" to \"{series}\" identified by \"{slot}\"\n";

    IReadOnlyList<SemanticGraphSpecificationResult> Run(string steps)
    {
        var result = Bind($"{Source}      specification S\n{steps}");
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        return SemanticGraphRunner.RunAll(result.Value!.Model);
    }

    [Fact] void should_draw_an_edge_per_item() => Run($"{Defined(("fuel", First), ("speed", Second))}        then graph G\n{Edge(First, "fuel")}{Edge(Second, "speed")}").Single().Passed.ShouldBeTrue();
    [Fact] void should_create_a_node_per_item() => Run($"{Defined(("fuel", First), ("speed", Second))}        then graph G\n          has node Series \"{Second}\"\n").Single().Passed.ShouldBeTrue();
    [Fact] void should_carry_the_item_property_into_the_edge() => Run($"{Defined(("fuel", First))}        then graph G\n          has edge SCORES from \"{Owner}\" to \"{First}\" identified by \"fuel\"\n            slot = \"fuel\"\n").Single().Passed.ShouldBeTrue();
    [Fact] void should_draw_nothing_for_an_empty_collection() => Run($"        given Defined\n          for \"{Owner}\"\n          slots none\n        then graph G\n          no node Series \"{First}\"\n").Single().Passed.ShouldBeTrue();
    [Fact] void should_remove_only_the_edges_of_the_items_listed() => Run($"{Defined(("fuel", First), ("speed", Second))}        when append Retired\n          for \"{Owner}\"\n{Items(("fuel", First))}        then graph G\n{NoEdge(First, "fuel")}{Edge(Second, "speed")}").Single().Passed.ShouldBeTrue();

    [Fact]
    void should_select_esm_v41()
    {
        var result = Bind($"{Source}      specification S\n{Defined(("fuel", First))}        then graph G\n{Edge(First, "fuel")}");
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V41);
    }

    [Fact]
    void should_keep_an_earlier_version_for_a_model_that_does_not_iterate()
    {
        var plain =
            "command reads protected\nconcept SeriesId : Uuid\n" +
            "module M\n  feature F\n    slice StateView S\n" +
            "      event Defined\n        series SeriesId\n" +
            "      graph G\n        node Series\n          seriesId SeriesId key\n" +
            "      projection P => graph G\n        node Series\n          from Defined key series\n";
        var result = Bind(plain);
        result.Value.ShouldNotBeNull();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V38);
    }

    [Fact]
    void should_refuse_iterating_something_that_is_not_a_list_of_a_composite_type()
    {
        var result = Bind(Source.Replace("foreach slots key slots.series", "foreach note key $eventSourceId"));
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.UnsupportedGraphForEach);
    }

    [Fact]
    void should_refuse_iterating_a_collection_inside_another()
    {
        var result = new ScreenplayCompiler().Compile(Source.Replace("foreach slots key slots.series", "foreach slots.series key $eventSourceId"));
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.UnsupportedGraphForEach);
    }

    [Fact]
    void should_print_the_handler_as_it_was_written()
    {
        var syntax = new ScreenplayCompiler().Parse(Source).Value!;
        var printed = new Printing.ScreenplayPrinter().Print(syntax);
        printed.ShouldContain("from Defined foreach slots source $eventSourceId target slots.series identified by slots.slot");
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Printing.for_ScreenplayPrinter;

public class when_round_tripping_graph_collects : Specification
{
    readonly ScreenplayCompiler _compiler = new();
    string _source;
    string _printed;

    void Establish()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, "Screenplay.slnx"))) root = root.Parent!;
        _source = File.ReadAllText(Path.Combine(root.FullName, "Source/Screenplay/Compiler/Conformance/graph-collects.play"));
    }

    void Because() => _printed = new ScreenplayPrinter().Print(_compiler.Parse(_source).Value!);

    [Fact] void should_print_a_list_collect() => _printed.ShouldContain("groups = collect person -membership:MEMBER_OF-> group:Group");
    [Fact] void should_print_an_unnamed_edge() => _printed.ShouldContain("parent = collect group -PART_OF-> parent:Group");
    [Fact] void should_print_an_incoming_edge() => _printed.ShouldContain("parent = collect child <-PART_OF- parent:Group");
    [Fact] void should_print_the_row_under_the_collect() => _printed.ShouldContain("          groups = collect person -membership:MEMBER_OF-> group:Group\n            id = group.groupId");
    [Fact] void should_print_what_it_parses() => new ScreenplayPrinter().Print(_compiler.Parse(_printed).Value!).ShouldEqual(_printed);
    [Fact] void should_parse_what_it_prints_without_diagnostics() => _compiler.Parse(_printed).Diagnostics.ShouldBeEmpty();
}

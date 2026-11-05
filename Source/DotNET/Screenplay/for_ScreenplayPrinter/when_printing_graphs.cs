// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.given;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_graphs : Specification
{
    const string Specification =
        "specification S\n" +
        "  given graph Organization\n" +
        "    has node Person \"p\"\n" +
        "      name = \"Ada\"\n" +
        "  when append PersonRemoved\n" +
        "    for \"p\"\n" +
        "  then graph Organization\n" +
        "    no node Person \"p\"\n" +
        "    has edge MEMBER_OF from \"a\" to \"b\"\n" +
        "    no edge MEMBER_OF from \"a\" to \"c\" identified by \"i\"\n";

    string _source;
    string _printed;
    string _reprinted;

    void Because()
    {
        _source = GraphDocuments.Document(GraphDocuments.Graph, GraphDocuments.Events, GraphDocuments.Projection, Specification);
        var compiler = new ScreenplayCompiler();
        var printer = new ScreenplayPrinter();
        _printed = printer.Print(compiler.Parse(_source).Value!);
        _reprinted = printer.Print(compiler.Parse(_printed).Value!);
    }

    [Fact] void should_print_the_same_text_again() => _reprinted.ShouldEqual(_printed);
    [Fact] void should_print_the_graph_declaration() => _printed.ShouldContain(GraphDocuments.Indent(GraphDocuments.Graph).TrimEnd('\n'));
    [Fact] void should_print_the_graph_projection() => _printed.ShouldContain(GraphDocuments.Indent(GraphDocuments.Projection).TrimEnd('\n'));
    [Fact] void should_print_the_graph_steps() => _printed.ShouldContain(GraphDocuments.Indent(Specification).TrimEnd('\n'));

    [Fact]
    void should_round_trip_the_syntax_through_json()
    {
        var application = new ScreenplayCompiler().Parse(_source).Value!;
        SyntaxJson.StructurallyEqual(application, SyntaxJson.Deserialize(SyntaxJson.Serialize(application))).ShouldBeTrue();
    }
}

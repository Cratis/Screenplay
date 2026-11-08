// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.given;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Printing.for_ScreenplayPrinter;

public class when_round_tripping_sinks : Specification
{
    const string ReadModelSource = "sink Reporting for readmodels\nmodule M\n  feature F\n    slice StateView S\n      readmodel Row\n        id String\n        sink Reporting";

    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_preserve_a_read_model_sink_through_syntax_json_and_canonical_source()
    {
        var printed = RoundTrip(ReadModelSource);
        printed.ShouldContain("sink Reporting for readmodels");
        var reparsed = _compiler.Parse(printed);
        reparsed.Success.ShouldBeTrue();
        reparsed.Value!.Sinks.Single().Stores.ShouldEqual(SinkKind.ReadModels);
        reparsed.Value.Modules.Single().Features.Single().Slices.Single().ReadModels!.Single().Sink.ShouldEqual("Reporting");
        new ScreenplayPrinter().Print(reparsed.Value).ShouldEqual(printed);
    }

    [Fact]
    void should_preserve_a_graph_sink_through_syntax_json_and_canonical_source()
    {
        var printed = RoundTrip("sink People for graphs\n" + GraphDocuments.Document(GraphDocuments.Graph.Replace("graph Organization\n", "graph Organization\n  sink People\n", StringComparison.Ordinal)));
        printed.ShouldContain("sink People for graphs");
        var reparsed = _compiler.Parse(printed);
        reparsed.Success.ShouldBeTrue();
        reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Graphs.Single().Sink.ShouldEqual("People");
        new ScreenplayPrinter().Print(reparsed.Value).ShouldEqual(printed);
    }

    [Fact]
    void should_print_the_directive_once_before_properties()
    {
        var printed = new ScreenplayPrinter().Print(_compiler.Parse(ReadModelSource + "\n        sink Reporting").Value!);
        printed.Split('\n').Count(line => line.Trim() == "sink Reporting").ShouldEqual(1);
        (printed.IndexOf("        sink Reporting", StringComparison.Ordinal) < printed.IndexOf("id String", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_escape_a_property_named_sink()
    {
        var printed = new ScreenplayPrinter().Print(_compiler.Parse("module M\n  feature F\n    slice StateView S\n      readmodel Row\n        @sink Money").Value!);
        printed.ShouldContain("@sink Money");
        _compiler.Parse(printed).Value!.Modules.Single().Features.Single().Slices.Single().ReadModels!.Single().Properties.Single().Name.ShouldEqual("sink");
    }

    [Fact]
    void should_not_emit_a_directive_without_a_sink() =>
        new ScreenplayPrinter().Print(_compiler.Parse("module M\n  feature F\n    slice StateView S\n      readmodel Row\n        id String").Value!).Contains("sink", StringComparison.Ordinal).ShouldBeFalse();

    string RoundTrip(string source)
    {
        var parsed = _compiler.Parse(source);
        parsed.Success.ShouldBeTrue();
        var restored = (ApplicationSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(parsed.Value!));
        return new ScreenplayPrinter().Print(restored);
    }
}

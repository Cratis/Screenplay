// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_SinkParser;

public class when_parsing_sink_declarations : Specification
{
    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_parse_a_read_model_sink()
    {
        var sink = _compiler.Parse("sink Reporting for readmodels").Value!.Sinks.Single();
        sink.Name.ShouldEqual("Reporting");
        sink.Stores.ShouldEqual(SinkKind.ReadModels);
    }

    [Fact]
    void should_parse_a_graph_sink() => _compiler.Parse("sink People for graphs").Value!.Sinks.Single().Stores.ShouldEqual(SinkKind.Graphs);

    [Fact]
    void should_keep_declaration_order() => _compiler.Parse("sink B for graphs\nsink A for readmodels").Value!.Sinks.Select(sink => sink.Name).ShouldEqual(new[] { "B", "A" });

    [Fact]
    void should_have_no_sinks_by_default() => _compiler.Parse("concept Id : Uuid").Value!.Sinks.ShouldBeEmpty();

    [Theory]
    [InlineData("sink")]
    [InlineData("sink Reporting")]
    [InlineData("sink Reporting for")]
    [InlineData("sink Reporting for projections")]
    [InlineData("sink Reporting for readmodel")]
    [InlineData("sink Reporting readmodels")]
    [InlineData("sink 1Reporting for readmodels")]
    [InlineData("sink Reporting for readmodels extra")]
    void should_report_a_malformed_declaration(string line)
    {
        var result = _compiler.Parse(line);
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.InvalidSinkDeclaration);
        result.Value!.Sinks.ShouldBeEmpty();
    }

    [Fact]
    void should_report_a_nested_line() => _compiler.Parse("sink Reporting for readmodels\n  database \"x\"").Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSinkDeclaration);
}

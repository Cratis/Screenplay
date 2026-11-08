// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.given;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_GraphParser;

public class when_parsing_the_sink_directive : Specification
{
    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_default_to_no_sink() => Graph(GraphDocuments.Graph).Sink.ShouldBeNull();

    [Fact]
    void should_name_the_sink() => Graph(GraphDocuments.Graph.Replace("graph Organization\n", "graph Organization\n  sink People\n", StringComparison.Ordinal)).Sink.ShouldEqual("People");

    [Fact]
    void should_warn_on_a_repeated_identical_directive() =>
        Codes(GraphDocuments.Graph.Replace("graph Organization\n", "graph Organization\n  sink People\n  sink People\n", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.RepeatedSinkDirective);

    [Fact]
    void should_refuse_two_different_sinks() =>
        Codes(GraphDocuments.Graph.Replace("graph Organization\n", "graph Organization\n  sink People\n  sink Other\n", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.ConflictingSinkDirective);

    [Fact]
    void should_report_a_malformed_directive() =>
        Codes(GraphDocuments.Graph.Replace("graph Organization\n", "graph Organization\n  sink People Other\n", StringComparison.Ordinal)).ShouldContainOnly(DiagnosticCodes.InvalidSinkDeclaration);

    string[] Codes(string graph) => [.. _compiler.Parse(GraphDocuments.Document(graph)).Diagnostics.Select(diagnostic => diagnostic.Code)];

    GraphSyntax Graph(string graph) => _compiler.Parse(GraphDocuments.Document(graph)).Value!.Modules.Single().Features.Single().Slices.Single().Graphs.Single();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.given;

namespace Cratis.Screenplay.Parsing.for_SinkValidator;

public class when_validating_sinks : Specification
{
    const string ReadModel = "module M\n  feature F\n    slice StateView S\n      readmodel Row\n        sink {0}\n        id String\n";

    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_accept_a_read_model_in_its_sink() => Codes("sink Reporting for readmodels\n" + string.Format(ReadModel, "Reporting")).ShouldBeEmpty();

    [Fact]
    void should_accept_a_graph_in_its_sink() => Codes("sink People for graphs\n" + GraphInSink("People")).ShouldBeEmpty();

    [Fact]
    void should_refuse_a_duplicate_name() =>
        Codes("sink Reporting for readmodels\nsink Reporting for graphs\n" + string.Format(ReadModel, "Reporting")).ShouldEqual(new[] { DiagnosticCodes.DuplicateSink });

    [Fact]
    void should_refuse_a_name_that_differs_only_in_case() =>
        Codes("sink Reporting for readmodels\nsink reporting for readmodels\n" + string.Format(ReadModel, "Reporting")).ShouldEqual(new[] { DiagnosticCodes.DuplicateSink });

    [Fact]
    void should_refuse_an_unknown_sink_on_a_read_model() => Codes(string.Format(ReadModel, "Reporting")).ShouldEqual(new[] { DiagnosticCodes.UnknownSink });

    [Fact]
    void should_refuse_an_unknown_sink_on_a_graph() => Codes(GraphInSink("People")).ShouldEqual(new[] { DiagnosticCodes.UnknownSink });

    [Fact]
    void should_match_a_directive_exactly() =>
        Codes("sink Reporting for readmodels\n" + string.Format(ReadModel, "reporting")).ShouldEqual(new[] { DiagnosticCodes.UnknownSink, DiagnosticCodes.UnusedSink });

    [Fact]
    void should_refuse_a_graph_sink_on_a_read_model() =>
        Codes("sink People for graphs\n" + string.Format(ReadModel, "People")).ShouldEqual(new[] { DiagnosticCodes.SinkKindMismatch });

    [Fact]
    void should_refuse_a_read_model_sink_on_a_graph() =>
        Codes("sink Reporting for readmodels\n" + GraphInSink("Reporting")).ShouldEqual(new[] { DiagnosticCodes.SinkKindMismatch });

    [Fact]
    void should_warn_on_an_unused_sink()
    {
        var diagnostic = _compiler.Compile("sink Reporting for readmodels\n" + string.Format(ReadModel, "Reporting") + "sink Archive for readmodels\n").Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.UnusedSink);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        diagnostic.Message.ShouldContain("Archive");
    }

    [Fact]
    void should_report_unknown_sinks_at_the_directive() =>
        _compiler.Compile(string.Format(ReadModel, "Reporting")).Diagnostics.Single().Location.Line.ShouldEqual(5);

    string[] Codes(string source) => [.. _compiler.Compile(source).Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code)];

    static string GraphInSink(string sink) => GraphDocuments.Document(GraphDocuments.Graph.Replace("graph Organization\n", $"graph Organization\n  sink {sink}\n", StringComparison.Ordinal));
}

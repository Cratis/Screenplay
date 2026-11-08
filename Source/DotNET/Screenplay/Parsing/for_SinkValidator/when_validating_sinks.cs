// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.given;

namespace Cratis.Screenplay.Parsing.for_SinkValidator;

public class when_validating_sinks : Specification
{
    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_accept_a_read_model_in_its_sink() => Codes("sink Reporting for readmodels\n" + ReadModel("Reporting")).ShouldBeEmpty();

    [Fact]
    void should_accept_a_graph_in_its_sink() => Codes("sink People for graphs\n" + GraphInSink("People")).ShouldBeEmpty();

    [Fact]
    void should_refuse_a_duplicate_name() =>
        Codes("sink Reporting for readmodels\nsink Reporting for graphs\n" + ReadModel("Reporting")).ShouldContainOnly(DiagnosticCodes.DuplicateSink);

    [Fact]
    void should_refuse_a_name_that_differs_only_in_case() =>
        Codes("sink Reporting for readmodels\nsink reporting for readmodels\n" + ReadModel("Reporting")).ShouldContainOnly(DiagnosticCodes.DuplicateSink);

    [Fact]
    void should_read_an_undeclared_read_model_sink_as_a_property() => Codes(ReadModel("Reporting")).ShouldNotContain(DiagnosticCodes.UnknownSink);

    [Fact]
    void should_refuse_an_unknown_sink_on_a_graph() => Codes(GraphInSink("People")).ShouldContainOnly(DiagnosticCodes.UnknownSink);

    [Fact]
    void should_match_a_directive_exactly() =>
        SinkCodes("sink Reporting for readmodels\n" + ReadModel("reporting")).ShouldContainOnly(DiagnosticCodes.SinkNameCaseMismatch, DiagnosticCodes.UnusedSink);

    [Fact]
    void should_refuse_a_graph_sink_on_a_read_model() =>
        Codes("sink People for graphs\n" + ReadModel("People")).ShouldContainOnly(DiagnosticCodes.SinkKindMismatch);

    [Fact]
    void should_refuse_a_read_model_sink_on_a_graph() =>
        Codes("sink Reporting for readmodels\n" + GraphInSink("Reporting")).ShouldContainOnly(DiagnosticCodes.SinkKindMismatch);

    [Fact]
    void should_warn_on_an_unused_sink()
    {
        var diagnostic = _compiler.Compile("sink Reporting for readmodels\n" + ReadModel("Reporting") + "sink Archive for readmodels\n").Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.UnusedSink);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        diagnostic.Message.ShouldContain("Archive");
    }

    [Fact]
    void should_report_unknown_sinks_at_the_directive() =>
        _compiler.Compile(GraphInSink("People")).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownSink).Location.Line.ShouldEqual(10);

    [Fact]
    void should_resolve_a_directive_against_a_sink_another_file_declares()
    {
        var sources = new Files.InMemoryPlayDocumentSource(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["application.play"] = "sink Reporting for readmodels\nimport \"module.play\"\n",
            ["module.play"] = ReadModel("Reporting")
        });
        var (_, compilation) = Files.PlayApplicationAssembly.Compile(new ScreenplayCompiler(), ["application.play"], sources);
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).ShouldBeEmpty();
        var readModel = compilation.Value!.Modules.Single().Features.Single().Slices.Single().ReadModels!.Single();
        readModel.Sink.ShouldEqual("Reporting");
        readModel.Properties.Select(property => property.Name).ShouldContainOnly("id");
    }

    string[] SinkCodes(string source) => [.. Codes(source).Where(code => code.CompareTo(DiagnosticCodes.InvalidSinkDeclaration) >= 0)];

    string[] Codes(string source) => [.. _compiler.Compile(source).Diagnostics.Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Information).Select(diagnostic => diagnostic.Code)];

    static string ReadModel(string sink) => $"module M\n  feature F\n    slice StateView S\n      readmodel Row\n        sink {sink}\n        id String\n";

    static string GraphInSink(string sink) => GraphDocuments.Document(GraphDocuments.Graph.Replace("graph Organization\n", $"graph Organization\n  sink {sink}\n", StringComparison.Ordinal));
}

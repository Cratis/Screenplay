// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ReadModelParser;

public class when_parsing_the_sink_directive : Specification
{
    const string Declared = "sink Reporting for readmodels\nsink Archive for readmodels\n";
    const string Prefix = "module M\n  feature F\n    slice StateView S\n      readmodel Row\n";
    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_default_to_no_sink() => Model(Declared, "        id String").Sink.ShouldBeNull();

    [Fact]
    void should_name_a_declared_sink() => Model(Declared, "        sink Reporting\n        id String").Sink.ShouldEqual("Reporting");

    [Fact]
    void should_take_the_directive_out_of_the_properties() => Model(Declared, "        sink Reporting\n        id String").Properties.Select(property => property.Name).ShouldContainOnly("id");

    [Fact]
    void should_accept_the_sink_after_properties() => Model(Declared, "        id String\n        sink Reporting").Sink.ShouldEqual("Reporting");

    [Fact]
    void should_record_where_the_directive_is() => Model(Declared, "        id String\n        sink Reporting").DirectiveLocations["sink"].Line.ShouldEqual(8);

    [Theory]
    [InlineData("", "sink Customer", "Customer")]
    [InlineData(Declared, "sink Customer", "Customer")]
    [InlineData(Declared, "sink String", "String")]
    [InlineData(Declared, "sink Reporting[]", "Reporting")]
    [InlineData(Declared, "sink Reporting optional", "Reporting")]
    [InlineData(Declared, "sink Reporting key", "Reporting")]
    [InlineData(Declared, "@sink Reporting", "Reporting")]
    void should_keep_a_property_named_sink_that_names_no_declared_sink(string declarations, string line, string type)
    {
        var result = _compiler.Parse(declarations + Prefix + $"        {line}");
        result.Diagnostics.Where(diagnostic => diagnostic.Code.CompareTo(DiagnosticCodes.InvalidSinkDeclaration) >= 0).ShouldBeEmpty();
        var model = ReadModel(result.Value!);
        model.Sink.ShouldBeNull();
        var property = model.Properties.Single();
        property.Name.ShouldEqual("sink");
        property.Type.Name.ShouldEqual(type);
    }

    [Fact]
    void should_warn_on_a_repeated_identical_directive()
    {
        var result = _compiler.Parse(Declared + Prefix + "        sink Reporting\n        id String\n        sink Reporting\n        sink Archive\n" + "      readmodel Other\n        sink Archive");
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RepeatedSinkDirective);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        diagnostic.Location.Line.ShouldEqual(9);
    }

    [Fact]
    void should_refuse_two_different_declared_sinks()
    {
        var result = _compiler.Parse(Declared + Prefix + "        sink Reporting\n        sink Archive\n        id String");
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.ConflictingSinkDirective);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        ReadModel(result.Value!).Sink.ShouldEqual("Reporting");
    }

    [Fact]
    void should_refuse_a_sink_on_a_passive_read_model()
    {
        var diagnostic = _compiler.Parse(Declared + Prefix + "        passive\n        sink Reporting\n        id String").Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.PassiveReadModelSink);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
    }

    [Fact]
    void should_warn_when_the_type_differs_only_in_case_from_a_sink()
    {
        var result = _compiler.Compile(Declared + Prefix + "        sink reporting");
        var diagnostic = result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.SinkNameCaseMismatch);
        diagnostic.Code.ShouldEqual(DiagnosticCodes.SinkNameCaseMismatch);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        ReadModel(result.Value!).Properties.Single().Name.ShouldEqual("sink");
    }

    ReadModelSyntax Model(string declarations, string body) => ReadModel(_compiler.Parse(declarations + Prefix + body).Value!);

    static ReadModelSyntax ReadModel(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().ReadModels!.First();
}

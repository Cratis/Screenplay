// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ReadModelParser;

public class when_parsing_the_sink_directive : Specification
{
    const string Prefix = "module M\n  feature F\n    slice StateView S\n";
    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_default_to_no_sink() => Model("        id String").Sink.ShouldBeNull();

    [Fact]
    void should_name_the_sink() => Model("        sink Reporting\n        id String").Sink.ShouldEqual("Reporting");

    [Fact]
    void should_accept_the_sink_after_properties() => Model("        id String\n        sink Reporting").Sink.ShouldEqual("Reporting");

    [Fact]
    void should_record_where_the_directive_is() => Model("        id String\n        sink Reporting").DirectiveLocations["sink"].Line.ShouldEqual(6);

    [Theory]
    [InlineData("sink String", "String")]
    [InlineData("sink Int", "Int")]
    [InlineData("sink Money[]", "Money")]
    [InlineData("sink Money?", "Money")]
    [InlineData("sink Money optional", "Money")]
    [InlineData("@sink Money", "Money")]
    void should_keep_a_property_named_sink(string line, string type)
    {
        var model = Model($"        {line}");
        model.Sink.ShouldBeNull();
        var property = model.Properties.Single();
        property.Name.ShouldEqual("sink");
        property.Type.Name.ShouldEqual(type);
    }

    [Fact]
    void should_warn_on_a_repeated_identical_directive()
    {
        var result = _compiler.Parse(Prefix + "      readmodel Row\n        sink Reporting\n        id String\n        sink Reporting");
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.RepeatedSinkDirective);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        ReadModel(result.Value!).Sink.ShouldEqual("Reporting");
    }

    [Fact]
    void should_refuse_two_different_sinks()
    {
        var result = _compiler.Parse(Prefix + "      readmodel Row\n        sink Reporting\n        sink Archive\n        id String");
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.ConflictingSinkDirective);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
        ReadModel(result.Value!).Sink.ShouldEqual("Reporting");
    }

    [Fact]
    void should_refuse_a_sink_on_a_passive_read_model()
    {
        var result = _compiler.Parse(Prefix + "      readmodel Row\n        passive\n        sink Reporting\n        id String");
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.PassiveReadModelSink);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Error);
    }

    [Fact]
    void should_report_a_bare_sink() => _compiler.Parse(Prefix + "      readmodel Row\n        sink\n        id String").Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSinkDeclaration);

    ReadModelSyntax Model(string body) => ReadModel(_compiler.Parse(Prefix + "      readmodel Row\n" + body).Value!);

    static ReadModelSyntax ReadModel(ApplicationSyntax application) => application.Modules.Single().Features.Single().Slices.Single().ReadModels!.Single();
}

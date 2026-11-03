// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ReadModelParser;

public class when_parsing_the_passive_directive : Specification
{
    const string Prefix = "module M\n  feature F\n    slice StateView S\n";
    readonly ScreenplayCompiler _compiler = new();

    [Fact]
    void should_default_to_active() => Model("        id String").IsPassive.ShouldBeFalse();

    [Fact]
    void should_mark_the_read_model_passive() => Model("        passive\n        id String").IsPassive.ShouldBeTrue();

    [Fact]
    void should_accept_passive_after_properties() => Model("        id String\n        passive").IsPassive.ShouldBeTrue();

    [Fact]
    void should_accept_passive_alongside_description_and_documentation() => Model("        description \"Rows\"\n        passive\n        documentation\n          ```markdown\n          Decisions\n          ```\n        id String").IsPassive.ShouldBeTrue();

    [Fact]
    void should_keep_a_property_named_passive()
    {
        var model = Model("        passive Bool");
        model.IsPassive.ShouldBeFalse();
        model.Properties.Single().Name.ShouldEqual("passive");
    }

    [Fact]
    void should_warn_on_repeated_passive()
    {
        var result = _compiler.Parse(Prefix + "      readmodel Row\n        passive\n        id String\n        passive");
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.RepeatedPassiveDirective);
        diagnostic.Severity.ShouldEqual(DiagnosticSeverity.Warning);
        result.Value!.Modules.Single().Features.Single().Slices.Single().ReadModels!.Single().IsPassive.ShouldBeTrue();
    }

    [Fact]
    void should_refuse_passive_on_the_header() => _compiler.Parse(Prefix + "      readmodel Row passive\n        id String").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidReadModelDeclaration).ShouldBeTrue();

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("  true")]
    [InlineData("\tfalse")]
    void should_refuse_passive_with_arguments(string argument) => _compiler.Parse(Prefix + $"      readmodel Row\n        passive {argument}").Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidPropertyDeclaration);

    [Theory]
    [InlineData("event E")]
    [InlineData("command C")]
    void should_not_accept_passive_in_an_event_or_command_body(string owner) => _compiler.Parse(Prefix + $"      {owner}\n        passive").Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();

    ReadModelSyntax Model(string body) => _compiler.Parse(Prefix + "      readmodel Row\n" + body).Value!.Modules.Single().Features.Single().Slices.Single().ReadModels!.Single();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_rejecting_syntax_only_responses : given.a_semantic_binder
{
    const string Prefix = "concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n";

    [Theory]
    [InlineData("      command C\n        id Id generated", 6)]
    [InlineData("      command C\n        id Id\n        returns id", 7)]
    [InlineData("      command C\n        id Id\n        returns\n          value = id", 7)]
    [InlineData("      specification S\n        when C\n          generated receipt = \"22222222-2222-2222-2222-222222222222\"", 7)]
    [InlineData("      specification S\n        when C\n        then returns \"accepted\"", 7)]
    [InlineData("      specification S\n        when C\n        then returns\n          value = \"accepted\"", 7)]
    void should_fail_before_binding_any_semantic_properties(string body, int line)
    {
        var result = Bind(Prefix + body);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        var diagnostic = result.Diagnostics.Single();
        diagnostic.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
        diagnostic.Location.Line.ShouldEqual(line);
        diagnostic.Message.ShouldEqual("Generated values, responses and return expectations are not admitted by any supported executable model (ESM) version yet (#300/#303).");
    }

    [Fact]
    void should_admit_the_legacy_tab_separated_property_without_creating_a_response()
    {
        const string Source = "concept id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id id identifier\n        returns\tid";
        var syntax = new ScreenplayCompiler().Compile(Source);
        syntax.Success.ShouldBeTrue();
        syntax.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Response.ShouldBeNull();
        var result = Bind(Source);
        result.Success.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268").ShouldBeFalse();
    }

    [Theory]
    [InlineData("returns String")]
    [InlineData("@returns String")]
    void should_preserve_the_legacy_model_for_deeper_members_after_a_returns_property(string property)
    {
        var nested = Bind(Prefix + "      command C\n        " + property + "\n          value Int");
        var ordinary = Bind(Prefix + "      command C\n        @returns String\n        value Int");
        nested.Success.ShouldBeTrue();
        ordinary.Success.ShouldBeTrue();
        Serialization.SemanticModelSerializer.Serialize(nested.Value!.Model)
            .SequenceEqual(Serialization.SemanticModelSerializer.Serialize(ordinary.Value!.Model)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("42")]
    [InlineData("\"11111111-1111-1111-1111-111111111111\"")]
    void should_not_admit_generated_identifier_fixtures_through_binding(string value)
    {
        var result = Bind(Prefix + $"      command C\n        id Id generated identifier\n      specification Fixture\n        when C\n          for {value}");
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.UnsupportedSemanticSyntax);
    }

    [Fact]
    void should_reject_programmatically_constructed_trees()
    {
        var syntax = new ScreenplayCompiler().Parse(Prefix + "      command C\n        id Id").Value!;
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var command = slice.Commands.Single();
        var location = new SourceLocation(42, 3, "programmatic.play");
        command = command with { Properties = [command.Properties.Single() with { IsGenerated = true, Location = location }], Response = new ScalarCommandResponseSyntax(new("id", location), location) };
        var specification = new SpecificationSyntax("Accepts", [], new("C", [], location) { GeneratedValues = [new("receipt", new LiteralExpressionSyntax("value", location), location)] }, [], [], location) { ThenReturns = new ScalarSpecificationReturnSyntax(new LiteralExpressionSyntax("value", location), location) };
        syntax = syntax with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [command], Specifications = [specification] }] }] }] };
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("programmatic"), "programmatic", "programmatic.play", string.Empty);
        var result = _binder.Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog));
        result.Value.ShouldBeNull();
        result.Diagnostics.Count().ShouldEqual(4);
        result.Diagnostics.All(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Location == location).ShouldBeTrue();
    }
}

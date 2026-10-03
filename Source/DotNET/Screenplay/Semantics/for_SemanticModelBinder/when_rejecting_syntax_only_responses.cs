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
        diagnostic.Message.ShouldEqual("This construct is not admitted by any supported ESM version; decision 0023 allocates it to ESM v8.");
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

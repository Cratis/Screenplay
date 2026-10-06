// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_rejecting_syntax_only_operations : given.a_semantic_binder
{
    const string Prefix = "module M\n  feature F\n    slice StateChange S\n";

    [Theory]
    [InlineData("system Mailer\n")]
    [InlineData(Prefix + "      operation Send\n        uses Mailer\n")]
    [InlineData(Prefix + "      command C\n        produces operation Send\n          uses Mailer\n          execute\n            implementation\n              hint \"Pending\"\n")]
    [InlineData(Prefix + "      specification T\n        given operation Send fails\n")]
    [InlineData(Prefix + "      specification T\n        then operation Send\n")]
    [InlineData(Prefix + "      specification T\n        then compensated Send\n")]
    void should_reject_every_new_construct_before_creating_a_model(string source)
    {
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Diagnostics.ShouldNotBeEmpty();
        result.Diagnostics.All(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("not admitted by any supported executable model (ESM) version yet (#301)", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_reject_programmatic_specification_nodes_before_event_lowering()
    {
        var syntax = new ScreenplayCompiler().Parse(Prefix + "      command C\n        produces event Recorded\n").Value!;
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var location = new SourceLocation(42, 3, "programmatic.play");
        var specification = new SpecificationSyntax("Test", [], new("C", [], location), [], [], location)
        {
            GivenOperationFailures = [new("Send", location)],
            ThenOperations = [new("Send", [], location)],
            ThenCompensated = [new("Send", location)]
        };
        syntax = syntax with { Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [specification] }] }] }] };
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("programmatic"), "programmatic", "programmatic.play", string.Empty);
        var result = _binder.Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog));
        result.Value.ShouldBeNull();
        result.Diagnostics.Count().ShouldEqual(3);
        result.Diagnostics.All(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Location == location && diagnostic.Message.Contains("not admitted by any supported executable model (ESM) version yet (#301)", StringComparison.Ordinal)).ShouldBeTrue();
    }
}

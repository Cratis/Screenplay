// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_rejecting_syntax_only_streams : given.a_semantic_binder
{
    [Theory]
    [InlineData("eventsource Account")]
    [InlineData("eventsource Account\n  stream Transactions")]
    [InlineData("eventsource Account\n  stream Transactions\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.Transactions")]
    [InlineData("eventsource Account\n  stream Transactions\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.Transactions\n        handler\n          file Unknown.cs")]
    void should_reject_authoring_before_attachment_or_identity_lowering(string source)
    {
        var result = Bind(source);
        result.Value.ShouldBeNull();
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldNotBeEmpty();
        result.Diagnostics.All(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Message.Contains("ESM v10", StringComparison.Ordinal)).ShouldBeTrue();
        result.ImplementationRequirements.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_reject_programmatic_declarations_and_route_only_commands(bool declaration)
    {
        var location = new SourceLocation(42, 3, "programmatic.play");
        var syntax = new ScreenplayCompiler().Parse("module M\n  feature F\n    slice StateChange S\n      command C").Value!;
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        if (declaration)
        {
            syntax = syntax with { EventSources = [new("Account", location) { Streams = [new("Transactions", location)] }] };
        }
        else
        {
            syntax = syntax with { Modules = [module with { Features = [feature with { Slices = [slice with { Commands = [slice.Commands.Single() with { Stream = new("Account", "Transactions", location) }] }] }] }] };
        }
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("programmatic"), "programmatic", "programmatic.play", string.Empty);
        var result = _binder.Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog));
        result.Value.ShouldBeNull();
        result.Diagnostics.Count().ShouldEqual(declaration ? 2 : 1);
        result.Diagnostics.All(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Location == location && diagnostic.Message.Contains("ESM v10", StringComparison.Ordinal)).ShouldBeTrue();
        result.ImplementationRequirements.ShouldBeEmpty();
    }
}

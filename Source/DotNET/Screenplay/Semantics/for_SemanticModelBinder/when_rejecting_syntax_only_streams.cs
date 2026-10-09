// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_rejecting_syntax_only_streams : given.a_semantic_binder
{
    [Theory]
    [InlineData("eventsource Account")]
    [InlineData("eventsource Account\n  stream Ledger\n    streamId\n      account String\n      period String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.Ledger\n          streamId\n            period = \"2026-10\"\n            account = \"a\"")]
    [InlineData("eventsource Account\n  stream Transactions")]
    [InlineData("eventsource Account\n  stream Transactions\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.Transactions")]
    void should_admit_declarations_and_declarative_routes_with_catalog_identities(string source)
    {
        var result = Bind(source + "\n");
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        result.Diagnostics.ShouldBeEmpty();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
        var account = result.Value.Model.Application.EventSources.Single();
        var address = SemanticAddress.ForEventSource(_applicationIdentity, account.Name);
        result.Value.Documents.IdentityCatalog.ResolveSemantic(address).ShouldEqual(account.Id);
        foreach (var stream in account.Streams)
        {
            result.Value.Documents.IdentityCatalog.ResolveSemantic(SemanticAddress.ForEventStream(address, stream.Name)).ShouldEqual(stream.Id);
        }
        result.ImplementationRequirements.ShouldBeEmpty();
    }

    [Fact]
    void should_still_refuse_a_routed_handler()
    {
        var result = Bind("eventsource Account\n  stream Transactions\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        stream Account.Transactions\n        handler\n          file Unknown.cs");
        result.Value.ShouldBeNull();
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("handler", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_admit_programmatic_declarations_but_require_a_declared_route_target(bool declaration)
    {
        var location = new SourceLocation(1, 1, "programmatic.play");
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
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("programmatic"), "programmatic", "programmatic.play", "module M\n  feature F\n    slice StateChange S\n      command C\n");
        var result = _binder.Bind("Projects", syntax, SemanticDocumentSet.Create([document], catalog));
        Assert.True(result.Success == declaration, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        if (declaration)
        {
            result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
            result.Value.Model.Application.EventSources.Single().Streams.Single().Name.ShouldEqual("Transactions");
            result.Diagnostics.ShouldBeEmpty();
        }
        else
        {
            result.Value.ShouldBeNull();
            result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Location == location).ShouldBeTrue();
        }
        result.ImplementationRequirements.ShouldBeEmpty();
    }
}

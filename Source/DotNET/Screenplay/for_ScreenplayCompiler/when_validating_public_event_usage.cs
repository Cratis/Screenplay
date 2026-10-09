// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_validating_public_event_usage : given.a_compiler
{
    const string Contracts = "import Outside.Arrived from \"other/store\"\nmodule Sales\n  feature Orders\n    slice StateChange Facts\n      event Changed\n      public event Published\n      public event Second\n";

    [Theory]
    [InlineData("StateChange", "command Send\n        produces Published", DiagnosticCodes.CommandProducesPublicEvent)]
    [InlineData("StateChange", "command Send\n        produces Facts.Published", DiagnosticCodes.CommandProducesPublicEvent)]
    [InlineData("StateView", "projection View\n        all", DiagnosticCodes.ForeignPublicEventConsumer)]
    [InlineData("StateView", "projection View\n        remove with Arrived", DiagnosticCodes.ForeignPublicEventConsumer)]
    [InlineData("StateView", "projection View\n        join related on id\n          with Arrived", DiagnosticCodes.ForeignPublicEventConsumer)]
    [InlineData("StateView", "reducer View => View\n        on Arrived\n          file Receive.cs", DiagnosticCodes.ForeignPublicEventConsumer)]
    [InlineData("StateChange", "constraint Unique\n        unique event Arrived", DiagnosticCodes.ForeignPublicEventConsumer)]
    [InlineData("StateChange", "constraint Unique\n        unique event Changed\n        released by Arrived", DiagnosticCodes.ForeignPublicEventConsumer)]
    [InlineData("Translate", "direction inbound\n      capture Data\n        append Published", DiagnosticCodes.InboundTranslationOutput)]
    [InlineData("Automation", "reaction Send\n        when Changed\n          produces Published", DiagnosticCodes.PublicEventRequiresOutboundTranslation)]
    [InlineData("Automation", "reaction Receive\n        when Arrived\n          produces Changed", DiagnosticCodes.ForeignPublicEventConsumer)]
    [InlineData("StateView", "projection View\n        from Arrived", DiagnosticCodes.ForeignPublicEventConsumer)]
    [InlineData("Translate", "direction outbound\n      reaction Send\n        when Published\n          produces Published", DiagnosticCodes.OutboundTranslationInput)]
    [InlineData("Translate", "direction inbound\n      reaction Receive\n        when Arrived\n          produces Published", DiagnosticCodes.InboundTranslationOutput)]
    [InlineData("Translate", "direction inbound\n      reaction Receive\n        when Arrived\n          produces Arrived", DiagnosticCodes.ForeignPublicEventProduced)]
    [InlineData("Translate", "reaction Receive\n        when Arrived\n          produces Changed", DiagnosticCodes.PublicTranslationRequiresDirection)]
    [InlineData("Translate", "direction outbound", DiagnosticCodes.OutboundPublicEventCount)]
    [InlineData("Translate", "direction outbound\n      reaction Send\n        when Changed\n          produces Published\n          produces Second", DiagnosticCodes.OutboundPublicEventCount)]
    [InlineData("Translate", "direction outbound\n      capture Data\n        append Published", DiagnosticCodes.TranslationConstructDirection)]
    [InlineData("Translate", "direction inbound\n      reaction Receive\n        when Changed\n          produces Changed", DiagnosticCodes.InboundTranslationInput)]
    [InlineData("Translate", "direction outbound\n      reaction Send\n        when Changed\n          produces Published\n          produces Changed", DiagnosticCodes.OutboundTranslationOutput)]
    void should_report_each_boundary_violation(string kind, string body, string code)
    {
        var result = _compiler.Compile(Contracts + $"    slice {kind} Transfer\n      {body}\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == code && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }

    [Theory]
    [InlineData("direction inbound\n      reaction Receive\n        when Arrived\n          produces Changed")]
    [InlineData("direction outbound\n      reaction Send\n        when Changed\n          produces Published")]
    [InlineData("reaction Receive\n        when Changed\n          produces Changed")]
    [InlineData("direction outbound\n      reaction Send\n        when Changed\n          produces Published\n          produces Published")]
    [InlineData("direction inbound\n      reaction Receive\n        when Arrived\n          produces Changed\n          produces Changed")]
    void should_accept_inbound_outbound_and_legacy_translations(string body)
    {
        var result = _compiler.Compile(Contracts + $"    slice Translate Transfer\n      {body}\n");
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    }

    [Fact]
    void should_prefer_nearest_declaration_over_a_foreign_import()
    {
        var result = _compiler.Compile(Contracts + "    slice Translate Send\n      direction outbound\n      event Arrived\n      reaction Send\n        when Arrived\n          produces Published\n");
        result.Success.ShouldBeTrue();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.OutboundTranslationInput).ShouldBeFalse();
    }

    [Fact]
    void should_resolve_after_merging_files()
    {
        var contracts = _compiler.Parse(Contracts, "contracts.play");
        var consumer = _compiler.Parse("module Sales\n  feature Orders\n    slice Automation Receive\n      reaction Receive\n        when Arrived\n          produces Facts.Changed\n", "consumer.play");
        var result = PlayFolderMerge.Merge([contracts, consumer]);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ForeignPublicEventConsumer && diagnostic.Location.Path == "consumer.play").ShouldBeTrue();
    }

    [Theory]
    [InlineData("Published", DiagnosticCodes.PublicEventRequiresOutboundTranslation)]
    [InlineData("Arrived", DiagnosticCodes.ForeignPublicEventProduced)]
    void should_not_allow_seed_to_bypass_public_production(string name, string code)
    {
        var result = _compiler.Compile(Contracts + $"seed\n  for \"order-1\"\n    {name}\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeTrue();
    }

    [Fact]
    void should_not_let_an_operation_shadow_an_event_subscription()
    {
        var parsed = _compiler.Parse(Contracts + "    slice Automation Receive\n      reaction Receive\n        when Arrived\n          produces Changed\n").Value!;
        var module = parsed.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Last();
        var altered = slice with { Operations = [new("Arrived", "Outside", [], slice.Location)] };
        var application = parsed with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Take(1), altered] }] }] };
        var context = ParserContext.ForDiagnostics();
        ScreenplayValidator.Validate(application, context);
        context.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.ForeignPublicEventConsumer).ShouldBeTrue();
    }

    [Fact]
    void should_validate_qualified_programmatic_input()
    {
        var parsed = _compiler.Parse(Contracts + "    slice Translate Receive\n      direction inbound\n      reaction Receive\n        when Arrived\n          produces Changed\n").Value!;
        var module = parsed.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Last();
        var reaction = slice.Reactions.Single();
        var trigger = reaction.Triggers.Single();
        var altered = slice with { Reactions = [reaction with { Triggers = [trigger with { Source = new NamedTriggerSourceSyntax("Sales.Orders.Facts.Changed", trigger.Location) }] }] };
        var application = parsed with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Take(1), altered] }] }] };
        var context = ParserContext.ForDiagnostics();
        ScreenplayValidator.Validate(application, context);
        context.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InboundTranslationInput).ShouldBeTrue();
    }

    [Fact]
    void should_reuse_private_origin_invariant_for_programmatic_input()
    {
        var parsed = _compiler.Parse(Contracts).Value!;
        var module = parsed.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var altered = slice with { Events = [slice.Events.First() with { Origin = "foreign" }, .. slice.Events.Skip(1)] };
        var application = parsed with { Modules = [module with { Features = [feature with { Slices = [altered] }] }] };
        var context = ParserContext.ForDiagnostics();
        ScreenplayValidator.Validate(application, context);
        context.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidEventDeclaration).ShouldBeTrue();
    }

    [Fact]
    void should_not_classify_ambiguous_references_as_private()
    {
        var result = _compiler.Compile(Contracts + "    slice StateChange Other\n      event Published\n    slice Translate Send\n      direction outbound\n      reaction Send\n        when Changed\n          produces Published\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AmbiguousReference).ShouldBeTrue();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.OutboundTranslationOutput).ShouldBeFalse();
    }

    [Fact]
    void should_not_classify_unresolved_outputs_as_private()
    {
        var result = _compiler.Compile(Contracts + "    slice Translate Receive\n      direction inbound\n      reaction Receive\n        when Arrived\n          produces Missing\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEvent).ShouldBeTrue();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InboundTranslationOutput).ShouldBeFalse();
    }

    [Fact]
    void should_preserve_semantic_refusal_and_validate_programmatic_syntax()
    {
        var parsed = _compiler.Parse(Contracts + "    slice StateChange Send\n      command Send\n        produces Changed\n").Value!;
        var module = parsed.Modules.Single();
        var feature = module.Features.Single();
        var send = feature.Slices.Last();
        var command = send.Commands.Single();
        var altered = send with { Commands = [command with { Produces = [command.Produces.Single() with { Event = "Published" }] }] };
        var application = parsed with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Take(1), altered] }] }] };
        var context = ParserContext.ForDiagnostics();
        ScreenplayValidator.Validate(application, context);
        context.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.CommandProducesPublicEvent).ShouldBeTrue();
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Sales"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("application"), "application", "application.play", Contracts);
        var documents = SemanticDocumentSet.Create([document], catalog);
        var bound = new SemanticModelBinder().Bind("Sales", application, documents);
        bound.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.CommandProducesPublicEvent).ShouldBeTrue();
        bound.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
    }
}

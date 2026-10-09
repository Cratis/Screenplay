// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Enforces public contract boundaries after the complete application has been assembled.
/// </summary>
internal static class PublicEventUsageValidator
{
    internal static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var resolver = new AuthoringProductionResolver(application);
        var slices = ScreenplayValidator.ScopedSlices(application).ToArray();
        if (!HasPublicContracts(resolver, application, slices))
        {
            return;
        }

        var events = new AuthoringProductionResolver(resolver.Declarations.Where(declaration => declaration.Kind == AuthoringProductionKind.Event));
        foreach (var (slice, scope) in slices)
        {
            ValidateSlice(application, slice, scope, resolver, events, context);
        }
        foreach (var seed in (application.Seeds ?? []).SelectMany(seed => seed.Groups).SelectMany(group => group.Events))
        {
            if (Resolve(application, events, [], seed.Event, seed, true, context) is not { Visibility: EventVisibility.Public } contract) continue;
            var code = contract.Origin is null ? DiagnosticCodes.PublicEventRequiresOutboundTranslation : DiagnosticCodes.ForeignPublicEventProduced;
            context.Error(code, $"Seed cannot append public event '{seed.Event}'; public production belongs to outbound translation, and foreign contracts cannot be produced locally.", seed.Location);
        }
    }

    /// <summary>
    /// Finds the projections and reducers whose <c>=&gt;</c> target resolves to an event rather than a read model.
    /// </summary>
    /// <param name="application">The assembled <see cref="ApplicationSyntax"/>.</param>
    /// <returns>The projection and reducer nodes that publish an event.</returns>
    internal static IReadOnlyList<SyntaxNode> EventTargets(ApplicationSyntax application)
    {
        var resolver = new AuthoringProductionResolver(application);
        var events = new AuthoringProductionResolver(resolver.Declarations.Where(declaration => declaration.Kind == AuthoringProductionKind.Event));
        var found = new List<SyntaxNode>();
        var diagnostics = ParserContext.ForDiagnostics();
        foreach (var (slice, scope) in ScreenplayValidator.ScopedSlices(application))
        {
            var readModels = (slice.ReadModels ?? []).Select(readModel => readModel.Name).ToHashSet(StringComparer.Ordinal);
            var targets = slice.Projections.Where(projection => projection.ReadModel is not null).Select(projection => (Target: projection.ReadModel!, Node: (SyntaxNode)projection))
                .Concat((slice.Reducers ?? []).Select(reducer => (Target: reducer.ReadModel, Node: (SyntaxNode)reducer)));
            found.AddRange(targets.Where(item => !readModels.Contains(item.Target) &&
                Resolve(application, events, scope.Segments, item.Target, item.Node, false, diagnostics) is not null).Select(item => item.Node));
        }

        return found;
    }

    static void ValidateSlice(ApplicationSyntax application, SliceSyntax slice, DeclarationScope scope, AuthoringProductionResolver resolver, AuthoringProductionResolver events, ParserContext context)
    {
        var collector = new PublicEventUsageCollector();
        collector.VisitSlice(slice);
        var contracts = new List<(SyntaxNode Node, EventVisibility Visibility, string? Origin, bool Output)>();
        var explicitOutbound = slice.Type == SliceType.Translate && slice.Direction == TranslationDirection.Outbound;
        var readModels = (slice.ReadModels ?? []).Select(readModel => readModel.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var (target, node) in collector.ProjectionTargets)
        {
            // '=> Name' names a read model unless it resolves to an event: then the projection or reducer publishes that event.
            if (readModels.Contains(target) || Resolve(application, events, scope.Segments, target, node, false, context) is not { } contract) continue;
            if (!explicitOutbound)
            {
                context.Error(DiagnosticCodes.EventTargetOutsideOutboundTranslation, $"'{target}' is an event: a projection or reducer may only target an event in an explicitly outbound Translate slice.", node.Location);
                continue;
            }

            contracts.Add((contract.Node, contract.Visibility, contract.Origin, true));
            ValidateUse(slice, target, node, true, false, contract.Visibility, contract.Origin, context);
        }

        // An outbound slice with a capture is already refused as a whole by the translation-direction check below.
        if (!(slice.Type == SliceType.Translate && slice.Direction == TranslationDirection.Inbound) && !explicitOutbound)
        {
            foreach (var source in collector.EventSources)
            {
                context.Error(DiagnosticCodes.EventsSourceOutsideInboundTranslation, $"'source events' reads another application's public events and belongs to a Translate slice with 'direction inbound', not '{slice.Name}'.", source.Location);
            }
        }

        foreach (var use in collector.Uses)
        {
            // Only 'produces' is an event/operation union. Subscriptions and capture appends name events.
            var candidates = use.Node is ProducesSyntax ? resolver : events;
            var target = Resolve(application, candidates, scope.Segments, use.Name, use.Node, use.Output, context);
            if (target is not { } contract) continue;
            contracts.Add((contract.Node, contract.Visibility, contract.Origin, use.Output));
            ValidateUse(slice, use.Name, use.Node, use.Output, use.Command, contract.Visibility, contract.Origin, context);
        }

        // 'all' really subscribes to every event type, unlike 'every', which only maps the projection's inputs.
        foreach (var all in collector.AllEvents)
        {
            foreach (var declaration in resolver.Declarations.Where(declaration => declaration.Node is EventSyntax))
            {
                var @event = (EventSyntax)declaration.Node;
                contracts.Add((@event, @event.Visibility, @event.Origin, false));
                ValidateUse(slice, @event.Name, all, false, false, @event.Visibility, @event.Origin, context);
            }
            foreach (var import in application.Imports.Where(import => import.Visibility == EventVisibility.Public))
            {
                contracts.Add((import, import.Visibility, import.Origin, false));
                ValidateUse(slice, import.QualifiedName, all, false, false, import.Visibility, import.Origin, context);
            }
        }

        if (slice.Type != SliceType.Translate) return;
        if (slice.Direction is null && (EventDeclarations.In(slice).Any(@event => @event.Visibility == EventVisibility.Public) || contracts.Exists(contract => contract.Visibility == EventVisibility.Public)))
        {
            context.Error(DiagnosticCodes.PublicTranslationRequiresDirection, "A Translate slice using public events requires an explicit 'direction inbound' or 'direction outbound'. Legacy translations without public metadata remain inbound.", slice.Location);
        }
        if (slice.Direction != TranslationDirection.Outbound) return;
        var outputs = contracts.Where(contract => contract.Output && contract.Visibility == EventVisibility.Public && contract.Origin is null)
            .Select(contract => contract.Node).Distinct(ReferenceEqualityComparer.Instance).Count();
        if (outputs != 1)
        {
            context.Error(DiagnosticCodes.OutboundPublicEventCount, $"Outbound Translate slice '{slice.Name}' must produce exactly one local public event type; found {outputs}.", slice.Location);
        }
        foreach (var capture in slice.Captures)
        {
            context.Error(DiagnosticCodes.TranslationConstructDirection, "An outbound translation consumes private local events, not an external-data capture. Capture belongs to inbound translation.", capture.Location);
        }
    }

    static bool HasPublicContracts(AuthoringProductionResolver resolver, ApplicationSyntax application, (SliceSyntax Slice, DeclarationScope Scope)[] slices) =>
        resolver.Declarations.Any(declaration => declaration.Node is EventSyntax { Visibility: EventVisibility.Public } or EventSyntax { Origin: not null }) ||
        application.Imports.Any(import => import.Visibility == EventVisibility.Public || import.Origin is not null) ||
        slices.Any(item => item.Slice.Direction is not null || UsesEventSource(item.Slice) || TargetsEvent(item.Slice, resolver));

    static bool TargetsEvent(SliceSyntax slice, AuthoringProductionResolver resolver) =>
        slice.Projections.Select(projection => projection.ReadModel).Concat((slice.Reducers ?? []).Select(reducer => reducer.ReadModel))
            .Any(target => target is not null && resolver.Declarations.Any(declaration => declaration.Kind == AuthoringProductionKind.Event && declaration.Name == target));

    static bool UsesEventSource(SliceSyntax slice) => slice.Captures.Any(capture => capture.Source is { } source && CaptureEventsSource.IsEvents(source));

    static void ValidateUse(SliceSyntax slice, string name, SyntaxNode node, bool output, bool command, EventVisibility visibility, string? origin, ParserContext context)
    {
        var foreign = origin is not null;
        var publicEvent = visibility == EventVisibility.Public;
        if (output)
        {
            if (command && publicEvent) context.Error(DiagnosticCodes.CommandProducesPublicEvent, $"Command cannot produce public event '{name}'; publish through an outbound Translate slice.", node.Location);
            if (slice.Type == SliceType.Translate && slice.Direction == TranslationDirection.Outbound && !publicEvent)
            {
                context.Error(DiagnosticCodes.OutboundTranslationOutput, $"Outbound translation must produce its one local public event, not private event '{name}'.", node.Location);
            }
            if (foreign) context.Error(DiagnosticCodes.ForeignPublicEventProduced, $"Foreign public event '{name}' cannot be produced locally; translate it into a private local event.", node.Location);
            if (publicEvent && !foreign && (slice.Type != SliceType.Translate || slice.Direction != TranslationDirection.Outbound))
            {
                context.Error(DiagnosticCodes.PublicEventRequiresOutboundTranslation, $"Local public event '{name}' may only be produced by an explicitly outbound Translate slice.", node.Location);
            }
            if (slice.Type == SliceType.Translate && slice.EffectiveDirection == TranslationDirection.Inbound && (publicEvent || foreign))
            {
                context.Error(DiagnosticCodes.InboundTranslationOutput, $"Inbound translation must produce private local events, not '{name}'.", node.Location);
            }
        }
        else
        {
            if (foreign && (slice.Type != SliceType.Translate || slice.Direction != TranslationDirection.Inbound))
            {
                context.Error(DiagnosticCodes.ForeignPublicEventConsumer, $"Foreign public event '{name}' may only be consumed by an explicitly inbound Translate slice.", node.Location);
            }
            if (slice.Type == SliceType.Translate && slice.Direction == TranslationDirection.Outbound && (publicEvent || foreign))
            {
                context.Error(DiagnosticCodes.OutboundTranslationInput, $"Outbound translation must consume private local events, not '{name}'.", node.Location);
            }
            if (slice.Type == SliceType.Translate && slice.Direction == TranslationDirection.Inbound && (!publicEvent || !foreign))
            {
                context.Error(DiagnosticCodes.InboundTranslationInput, $"Explicitly inbound translation must consume foreign public events, not '{name}'.", node.Location);
            }
        }
    }

    static (SyntaxNode Node, EventVisibility Visibility, string? Origin)? Resolve(ApplicationSyntax application, AuthoringProductionResolver resolver, IReadOnlyList<string> scope, string name, SyntaxNode use, bool output, ParserContext context)
    {
        var resolution = resolver.Resolve(name, scope);
        if (resolution.Declaration?.Node is EventSyntax @event) return (@event, @event.Visibility, @event.Origin);
        if (resolution.Kind == AuthoringProductionKind.Operation) return null;
        if (resolution.Kind == AuthoringProductionKind.Ambiguous)
        {
            context.Warning(DiagnosticCodes.AmbiguousReference, $"Ambiguous event reference '{name}'; qualify it with its owning module, feature and slice.", use.Location);
            return null;
        }
        var imports = application.Imports.Where(import => import.Name == name || import.QualifiedName == name).ToArray();
        if (imports is [var imported])
        {
            var local = resolver.Resolve(imported.QualifiedName, scope);
            if (local.Declaration?.Node is EventSyntax importedEvent) return (importedEvent, importedEvent.Visibility, importedEvent.Origin);
            if (local.Kind == AuthoringProductionKind.Ambiguous)
            {
                context.Warning(DiagnosticCodes.AmbiguousReference, $"Ambiguous imported event reference '{name}'; qualify its declaration.", use.Location);
                return null;
            }

            // An opaque legacy import has no event classification. Only explicit public metadata supplies one.
            return imported.Visibility == EventVisibility.Public ? (imported, imported.Visibility, imported.Origin) : null;
        }
        if (imports.Length > 1)
        {
            context.Warning(DiagnosticCodes.AmbiguousReference, $"Ambiguous imported event reference '{name}'; use its qualified name.", use.Location);
        }
        else if (((output && use is not CaptureAppendSyntax) || use is CaptureSourceSettingSyntax) && !context.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEvent && diagnostic.Location == use.Location))
        {
            context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{name}' - declare its contract before classifying its usage.", use.Location);
        }

        return null;
    }
}

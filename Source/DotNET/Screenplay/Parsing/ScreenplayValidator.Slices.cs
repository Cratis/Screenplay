// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenplayValidator
{
    /// <summary>
    /// Validates the range, uniqueness and consecutive numbering of event generations in one slice.
    /// </summary>
    static void ValidateEventGenerations(SliceSyntax slice, ParserContext context)
    {
        // The same event name denotes one contract only within one owning slice.
        foreach (var @event in EventDeclarations.In(slice).Where(@event => @event.Generation is 0 or uint.MaxValue))
        {
            context.Error(
                DiagnosticCodes.InvalidEventGeneration,
                $"Event '{@event.Name}' must declare a generation between 1 and {uint.MaxValue - 1}",
                @event.Location);
        }

        foreach (var group in EventDeclarations.In(slice).GroupBy(@event => @event.Name, StringComparer.Ordinal))
        {
            ReportDuplicateGenerations(group, slice, context);
            ReportMissingGenerations(group, slice, context);
        }
    }

    static void ReportDuplicateGenerations(IGrouping<string, EventSyntax> group, SliceSyntax slice, ParserContext context)
    {
        var unmarkedFirstGenerations = group.Count(@event => @event.Generation == 1 && !@event.HasGenerationMarker);
        foreach (var duplicate in group.GroupBy(@event => @event.Generation).SelectMany(generation => generation.Skip(1)))
        {
            var withoutMarker = duplicate.Generation == 1 && !duplicate.HasGenerationMarker && unmarkedFirstGenerations > 1;
            context.Error(
                DiagnosticCodes.DuplicateEventGeneration,
                withoutMarker
                    ? $"Event '{group.Key}' is declared more than once without a generation marker in slice '{slice.Name}' (both are generation 1)"
                    : $"Event '{group.Key}' declares generation {duplicate.Generation} more than once in slice '{slice.Name}'",
                duplicate.Location);
        }
    }

    static void ReportMissingGenerations(IGrouping<string, EventSyntax> group, SliceSyntax slice, ParserContext context)
    {
        var previous = 0u;
        foreach (var @event in group.Where(@event => @event.Generation is > 0 and < uint.MaxValue).OrderBy(@event => @event.Generation))
        {
            if (@event.Generation > previous && @event.Generation != previous + 1)
            {
                context.Error(
                    DiagnosticCodes.MissingEventGeneration,
                    $"Event '{group.Key}' in slice '{slice.Name}' is missing generation {previous + 1} before generation {@event.Generation}",
                    @event.Location);
            }

            previous = @event.Generation;
        }
    }

    // A unique constraint names properties the event declares directly. An event that is not declared here is
    // left to the unknown event check (an imported one has no properties to compare with), a property path is
    // left to the binder, and a property that only an earlier generation declares is left to the binder too,
    // which names the revision it was removed in.
    static void ValidateConstraintProperties(SliceSyntax slice, ILookup<string, EventSyntax> eventDeclarations, ParserContext context)
    {
        foreach (var rule in slice.Constraints.SelectMany(constraint => new[] { constraint }.Concat(constraint.AdditionalRules)).OfType<UniquePropertyConstraintSyntax>())
        {
            var declarations = eventDeclarations[rule.Event].ToArray();
            if (declarations.Length == 0)
            {
                continue;
            }

            foreach (var property in new[] { rule.Property }.Concat(rule.AdditionalProperties)
                .Where(property => !property.Contains('.', StringComparison.Ordinal) &&
                    !declarations.Any(declaration => declaration.Properties.Any(declared => declared.Name == property))))
            {
                context.Error(
                    DiagnosticCodes.UnknownConstraintProperty,
                    $"Constraint '{rule.Name}' names property '{property}', which event '{rule.Event}' does not declare.",
                    rule.Location);
            }
        }
    }

    static void ValidateSlice(SliceSyntax slice, DocumentNames names, AuthoringProductionResolver productionResolver, ParserContext context)
    {
        ValidateDeclaredTypes(slice, names, context);
        ValidateConcurrency(slice, context);
        foreach (var authorize in slice.Commands.Select(command => command.Authorize)
            .Concat(slice.Queries.Select(query => query.Authorize)))
        {
            ValidateAuthorize(authorize, names.Policies, context);
        }

        foreach (var produces in slice.Commands.SelectMany(command => command.Produces)
            .Where(produces => IsUnknownEventProduction(produces, slice, names, productionResolver)))
        {
            context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{produces.Event}' - declare it with 'event {produces.Event}'", produces.Location);
        }

        ValidateConstraintEvents(slice, names, context);
    }

    static bool IsUnknownEventProduction(ProducesSyntax produces, SliceSyntax slice, DocumentNames names, AuthoringProductionResolver productionResolver) =>
        productionResolver.IsEventProduction(produces, slice) &&
        !produces.Event.Contains('.', StringComparison.Ordinal) &&
        !names.Events.Contains(produces.Event);

    static void ValidateDeclaredTypes(SliceSyntax slice, DocumentNames names, ParserContext context)
    {
        foreach (var @event in EventDeclarations.In(slice))
        {
            ValidatePropertyTypes(@event.Properties, $"event '{@event.Name}'", names.Types, context);
        }

        foreach (var command in slice.Commands)
        {
            ValidatePropertyTypes(command.Properties, $"command '{command.Name}'", names.Types, context);
            var reads = new CommandReadValidator(command, context);
            reads.ValidateReads(names.ReadModels);
            reads.ValidateRequirements();
        }

        // The return type of a query names a read model, which no construct declares - only the
        // parameters resolve against the document's own types.
        foreach (var query in slice.Queries)
        {
            var parameters = (query.By is null ? [] : new[] { query.By }).Concat(query.ByParts).Concat(query.Filters)
                .Select(parameter => new PropertySyntax(parameter.Name, parameter.Type, parameter.Location));
            ValidatePropertyTypes(parameters, $"query '{query.Name}'", names.Types, context);
        }
    }

    static void ValidateConcurrency(SliceSyntax slice, ParserContext context)
    {
        foreach (var concurrency in slice.Commands.Select(command => command.Concurrency)
            .OfType<ConcurrencySyntax>()
            .Where(IsEmptyConcurrency))
        {
            context.Error(DiagnosticCodes.EmptyConcurrency, "Empty 'concurrency' block - declare at least one of eventSource, sourceType, streamType, streamId or events", concurrency.Location);
        }
    }

    static bool IsEmptyConcurrency(ConcurrencySyntax concurrency) =>
        concurrency is { EventSource: false, EventSourceType: null, EventStreamType: null, EventStreamId: null } &&
        !concurrency.EventTypes.Any();

    static void ValidateConstraintEvents(SliceSyntax slice, DocumentNames names, ParserContext context)
    {
        foreach (var constraint in slice.Constraints)
        {
            var @event = constraint switch
            {
                UniquePropertyConstraintSyntax unique => unique.Event,
                UniqueEventConstraintSyntax unique => unique.Event,
                _ => null
            };

            if (@event?.Length > 0 && !names.Events.Contains(@event))
            {
                context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{@event}' - declare it with 'event {@event}'", constraint.Location);
            }
        }
    }
}

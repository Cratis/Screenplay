// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates cross references in a parsed document - policies referenced by <c>authorize</c> and personas,
/// events referenced by reactors, <c>produces</c>, constraints and <c>seed</c> blocks, and the types
/// referenced by properties - and that <c>concurrency</c> and <c>seed</c> blocks are not empty and
/// <c>authentication</c> provider and <c>type</c> names are unique.
/// </summary>
internal static class ScreenplayValidator
{
    /// <summary>
    /// Validates an application and reports warnings for unknown references.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    public static void Validate(ApplicationSyntax application, ParserContext context)
    {
        var slices = application.Modules
            .SelectMany(module => module.Features.SelectMany(AllFeatures))
            .SelectMany(feature => feature.Slices)
            .ToList();

        var knownEvents = slices.SelectMany(slice => slice.Events.Select(@event => @event.Name))
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();
        var knownPolicies = application.Policies.Select(policy => policy.Name).ToHashSet();

        // A projection is what produces a read model, so its declared read model - or its own name when it
        // does not rename one - is what a command can say it reads.
        var knownReadModels = slices.SelectMany(slice => slice.Projections)
            .Select(projection => projection.ReadModel ?? projection.Name)
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();
        var knownTypes = ConceptSyntax.PrimitiveTypes
            .Concat(application.Concepts.Select(concept => concept.Name))
            .Concat((application.Types ?? []).Select(type => type.Name))
            .Concat(application.Imports.Select(import => import.Name))
            .ToHashSet();

        ValidateTypes(application, knownTypes, context);

        foreach (var persona in application.Personas ?? [])
        {
            foreach (var policy in persona.Policies.Where(policy => !knownPolicies.Contains(policy)))
            {
                context.Warning(DiagnosticCodes.UnknownPolicy, $"Unknown policy '{policy}' - declare it with 'policy {policy}'", persona.Location);
            }
        }

        foreach (var duplicate in (application.Authentication?.Providers ?? [])
            .GroupBy(provider => provider.Identity, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.DuplicateProvider, $"Duplicate provider '{duplicate.Identity}' - a provider must be distinguishable, so give one of them a name", duplicate.Location);
        }

        foreach (var seed in application.Seeds ?? [])
        {
            if (!seed.Groups.Any())
            {
                context.Error(DiagnosticCodes.EmptySeed, "Empty 'seed' block - declare at least one 'for' group", seed.Location);
            }

            foreach (var @event in seed.Groups.SelectMany(group => group.Events)
                .Where(@event => !knownEvents.Contains(@event.Event)))
            {
                context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{@event.Event}' - declare it with 'event {@event.Event}'", @event.Location);
            }
        }

        foreach (var slice in slices)
        {
            ValidateSlice(slice, knownEvents, knownPolicies, knownTypes, knownReadModels, context);
        }
    }

    static IEnumerable<FeatureSyntax> AllFeatures(FeatureSyntax feature) =>
        new[] { feature }.Concat(feature.Features.SelectMany(AllFeatures));

    static void ValidateTypes(ApplicationSyntax application, HashSet<string> knownTypes, ParserContext context)
    {
        var types = (application.Types ?? []).ToList();
        var declared = application.Concepts.Select(concept => concept.Name).Concat(types.Select(type => type.Name));

        foreach (var duplicate in declared.GroupBy(name => name, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key))
        {
            var location = types.Find(type => type.Name == duplicate)?.Location ??
                           application.Concepts.First(concept => concept.Name == duplicate).Location;
            context.Error(DiagnosticCodes.DuplicateDeclaration, $"Duplicate declaration of '{duplicate}' - concept and type names must be unique", location);
        }

        foreach (var type in types)
        {
            ValidatePropertyTypes(type.Properties, $"type '{type.Name}'", knownTypes, context);
        }
    }

    /// <summary>
    /// Warns for every property whose type reference names nothing the document declares.
    /// </summary>
    /// <param name="properties">The <see cref="PropertySyntax">properties</see> to check.</param>
    /// <param name="owner">The declaration the properties belong to, used in diagnostics.</param>
    /// <param name="knownTypes">The primitives, concepts, types and imports the document makes available.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A reference to a shape that lives outside the document makes the document depend on something a
    /// reader cannot see. It stays a warning because a runtime may still resolve the name - what matters
    /// is that the gap is visible.
    /// </remarks>
    static void ValidatePropertyTypes(
        IEnumerable<PropertySyntax> properties,
        string owner,
        HashSet<string> knownTypes,
        ParserContext context)
    {
        foreach (var property in properties.Where(property => !knownTypes.Contains(property.Type.Name)))
        {
            context.Warning(
                DiagnosticCodes.UnknownType,
                $"Unknown type '{property.Type.Name}' on '{property.Name}' of {owner} - declare it with 'concept {property.Type.Name} : <Primitive>' or 'type {property.Type.Name}'",
                property.Location);
        }
    }

    /// <summary>
    /// Validates that what a command reads exists, and that the key it reads by is one of its own properties.
    /// </summary>
    /// <param name="command">The <see cref="CommandSyntax"/> to validate.</param>
    /// <param name="knownReadModels">The read models the document's projections produce.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    static void ValidateReads(CommandSyntax command, HashSet<string> knownReadModels, ParserContext context)
    {
        var properties = command.Properties.Select(property => property.Name).ToHashSet();

        foreach (var reads in command.Reads ?? [])
        {
            if (!knownReadModels.Contains(reads.ReadModel))
            {
                context.Warning(
                    DiagnosticCodes.UnknownReadModel,
                    $"Unknown read model '{reads.ReadModel}' - no projection in the document produces it",
                    reads.Location);
            }

            if (reads.By is { } by && !properties.Contains(by))
            {
                context.Warning(
                    DiagnosticCodes.UnknownReadsKey,
                    $"Command '{command.Name}' reads '{reads.ReadModel}' by '{by}', which is not one of its properties",
                    reads.Location);
            }
        }
    }

    /// <summary>
    /// Validates that the operands of a command's requirements name something the command can see.
    /// </summary>
    /// <param name="command">The <see cref="CommandSyntax"/> to validate.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A requirement is only worth stating if a consumer can tell what it is about. An operand is either a
    /// property of the command or a path into state the command declares it reads - anything else names
    /// something no reader of the document can resolve.
    /// </remarks>
    static void ValidateRequirements(CommandSyntax command, ParserContext context)
    {
        var properties = command.Properties.Select(property => property.Name).ToHashSet();
        var reads = (command.Reads ?? []).Select(read => read.ReadModel).ToHashSet();

        foreach (var requirement in command.Validations.OfType<DeclarativeValidateSyntax>()
            .SelectMany(validate => validate.Requirements ?? []))
        {
            foreach (var operand in Operands(requirement.Condition))
            {
                var separator = operand.IndexOf('.', StringComparison.Ordinal);
                if (separator < 0)
                {
                    if (!properties.Contains(operand))
                    {
                        context.Warning(
                            DiagnosticCodes.UnknownRequirementOperand,
                            $"Command '{command.Name}' requires '{operand}', which is neither one of its properties nor state it reads",
                            requirement.Location);
                    }

                    continue;
                }

                var source = operand[..separator];
                if (!reads.Contains(source))
                {
                    context.Warning(
                        DiagnosticCodes.UnknownRequirementOperandSource,
                        $"Command '{command.Name}' requires '{operand}', but does not declare 'reads {source}'",
                        requirement.Location);
                }
            }
        }
    }

    /// <summary>
    /// Yields the left hand operand of every comparison in a condition.
    /// </summary>
    /// <param name="condition">The <see cref="ConditionSyntax"/> to walk.</param>
    /// <returns>The operands, in the order they appear.</returns>
    static IEnumerable<string> Operands(ConditionSyntax condition) => condition switch
    {
        ComparisonConditionSyntax comparison => [comparison.Left],
        LogicalConditionSyntax logical => Operands(logical.Left).Concat(Operands(logical.Right)),
        _ => []
    };

    static void ValidateSlice(
        SliceSyntax slice,
        HashSet<string> knownEvents,
        HashSet<string> knownPolicies,
        HashSet<string> knownTypes,
        HashSet<string> knownReadModels,
        ParserContext context)
    {
        foreach (var @event in slice.Events)
        {
            ValidatePropertyTypes(@event.Properties, $"event '{@event.Name}'", knownTypes, context);
        }

        foreach (var command in slice.Commands)
        {
            ValidatePropertyTypes(command.Properties, $"command '{command.Name}'", knownTypes, context);
            ValidateReads(command, knownReadModels, context);
            ValidateRequirements(command, context);
        }

        // The return type of a query names a read model, which no construct declares - only the
        // parameters resolve against the document's own types.
        foreach (var query in slice.Queries)
        {
            var parameters = (query.By is null ? [] : new[] { query.By }).Concat(query.Filters)
                .Select(parameter => new PropertySyntax(parameter.Name, parameter.Type, parameter.Location));
            ValidatePropertyTypes(parameters, $"query '{query.Name}'", knownTypes, context);
        }

        foreach (var concurrency in slice.Commands.Select(command => command.Concurrency)
            .OfType<ConcurrencySyntax>()
            .Where(concurrency => concurrency is { EventSource: false, EventSourceType: null, EventStreamType: null, EventStreamId: null } &&
                !concurrency.EventTypes.Any()))
        {
            context.Error(DiagnosticCodes.EmptyConcurrency, "Empty 'concurrency' block - declare at least one of eventSource, sourceType, streamType, streamId or events", concurrency.Location);
        }

        foreach (var authorize in slice.Commands.Select(command => command.Authorize)
            .Concat(slice.Queries.Select(query => query.Authorize))
            .OfType<AuthorizeSyntax>())
        {
            foreach (var policy in authorize.Policies.Where(policy => !knownPolicies.Contains(policy.Name)))
            {
                context.Warning(DiagnosticCodes.UnknownPolicy, $"Unknown policy '{policy.Name}' - declare it with 'policy {policy.Name}'", policy.Location);
            }
        }

        foreach (var produces in slice.Commands.SelectMany(command => command.Produces)
            .Where(produces => !knownEvents.Contains(produces.Event)))
        {
            context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{produces.Event}' - declare it with 'event {produces.Event}'", produces.Location);
        }

        foreach (var trigger in slice.Reactors.SelectMany(reactor => reactor.Triggers)
            .Where(trigger => !knownEvents.Contains(trigger.Event)))
        {
            context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{trigger.Event}' - declare it with 'event {trigger.Event}'", trigger.Location);
        }

        foreach (var constraint in slice.Constraints)
        {
            var @event = constraint switch
            {
                UniquePropertyConstraintSyntax unique => unique.Event,
                UniqueEventConstraintSyntax unique => unique.Event,
                _ => null
            };

            if (@event?.Length > 0 && !knownEvents.Contains(@event))
            {
                context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{@event}' - declare it with 'event {@event}'", constraint.Location);
            }
        }
    }
}

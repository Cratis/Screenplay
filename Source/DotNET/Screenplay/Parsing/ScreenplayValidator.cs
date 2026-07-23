// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates cross references in a parsed document - policies referenced by <c>authorize</c> and
/// events referenced by reactors, <c>produces</c> and constraints.
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

        foreach (var persona in application.Personas ?? [])
        {
            foreach (var policy in persona.Policies.Where(policy => !knownPolicies.Contains(policy)))
            {
                context.Warning($"Unknown policy '{policy}' - declare it with 'policy {policy}'", persona.Location);
            }
        }

        foreach (var slice in slices)
        {
            ValidateSlice(slice, knownEvents, knownPolicies, context);
        }
    }

    static IEnumerable<FeatureSyntax> AllFeatures(FeatureSyntax feature) =>
        new[] { feature }.Concat(feature.Features.SelectMany(AllFeatures));

    static void ValidateSlice(SliceSyntax slice, HashSet<string> knownEvents, HashSet<string> knownPolicies, ParserContext context)
    {
        foreach (var authorize in slice.Commands.Select(command => command.Authorize)
            .Concat(slice.Queries.Select(query => query.Authorize))
            .OfType<AuthorizeSyntax>())
        {
            foreach (var policy in authorize.Policies.Where(policy => !knownPolicies.Contains(policy.Name)))
            {
                context.Warning($"Unknown policy '{policy.Name}' - declare it with 'policy {policy.Name}'", policy.Location);
            }
        }

        foreach (var produces in slice.Commands.SelectMany(command => command.Produces)
            .Where(produces => !knownEvents.Contains(produces.Event)))
        {
            context.Warning($"Unknown event '{produces.Event}' - declare it with 'event {produces.Event}'", produces.Location);
        }

        foreach (var trigger in slice.Reactors.SelectMany(reactor => reactor.Triggers)
            .Where(trigger => !knownEvents.Contains(trigger.Event)))
        {
            context.Warning($"Unknown event '{trigger.Event}' - declare it with 'event {trigger.Event}'", trigger.Location);
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
                context.Warning($"Unknown event '{@event}' - declare it with 'event {@event}'", constraint.Location);
            }
        }
    }
}

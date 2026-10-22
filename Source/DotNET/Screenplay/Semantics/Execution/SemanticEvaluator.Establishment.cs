// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

public sealed partial class SemanticEvaluator
{
    // Specifications also retain legacy given events without an event source; their null destination must not
    // become a fabricated identity. All supplied identities, payloads, tags and projections use the same core.
    internal SemanticExecutionResult EstablishSpecificationWorld(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts) =>
        EstablishWorld(plan, facts, false);

    internal SemanticExecutionResult EstablishWorld(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts, bool publicReplay)
    {
        try
        {
            if (ValidateHistory(plan, facts, publicReplay) is { } rejection)
            {
                return rejection;
            }
        }
        catch (InvalidSemanticContract exception)
        {
            return new SemanticRejected(SemanticWorld.Empty, SemanticRejectionCategory.Contract, null, exception.Message);
        }

        var reducer = ReachedByHistory(plan, facts);
        if (publicReplay && reducer is not null)
        {
            return new SemanticUnsupported(
                SemanticWorld.Empty,
                SemanticExecutionCapability.Projection,
                $"Reducer '{reducer.Name}' has opaque transitions and requires a target provider to compute read-model state.");
        }

        try
        {
            if (!TryProject(plan, [], new ProjectionState([], []), facts, out var state, out var failure))
            {
                return new SemanticUnsupported(SemanticWorld.Empty, SemanticExecutionCapability.Projection, failure!);
            }

            return new SemanticAccepted(SemanticWorld.Create(facts, state.ReadModels).WithProjectionState(state.ReadModels, state.PendingChildren), facts, []);
        }
        catch (InvalidSemanticContract exception)
        {
            return new SemanticUnsupported(SemanticWorld.Empty, SemanticExecutionCapability.Projection, exception.Message);
        }
    }

    static SemanticRejected RejectedHistory(string message) =>
        new(SemanticWorld.Empty, SemanticRejectionCategory.Contract, null, message);

    static SemanticRejected? ValidateHistory(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts, bool publicReplay)
    {
        // Validate the entire history before any projection can observe an invalid occurrence.
        if (!facts.IsDefault && facts.Any(fact => IsMalformedOccurrence(plan, fact, publicReplay)))
        {
            return RejectedHistory("Fact occurrence metadata is malformed.");
        }

        SemanticWorld.Create(facts, []);
        var validator = new SemanticValueValidator(
            plan.Model.Application.Concepts.ToDictionary(concept => concept.Id),
            plan.Model.Application.Types.ToDictionary(type => type.Id));
        foreach (var fact in facts)
        {
            if (ValidateFact(plan, validator, fact, publicReplay) is { } rejection)
            {
                return rejection;
            }
        }

        return null;
    }

    static bool IsMalformedOccurrence(SemanticExecutionPlan plan, SemanticFact fact, bool publicReplay) =>
        HasMalformedEnvelope(fact) ||
        (plan.Model.SemanticVersion != SemanticVersion.V1 && HasMalformedContext(fact, publicReplay));

    static bool HasMalformedEnvelope(SemanticFact fact) =>
        fact is null ||
        (fact is { Tags.IsDefault: true } or { Context.EventSource: null }) ||
        (fact is { Tags: var tags } && tags.Any(string.IsNullOrWhiteSpace));

    static bool HasMalformedContext(SemanticFact fact, bool publicReplay) =>
        (publicReplay && fact.Context is null) ||
        (fact.Context is not null && (fact.Destination is SemanticNullValue ||
            (fact.Destination is SemanticTextValue text && string.IsNullOrWhiteSpace(text.Value))));

    static SemanticRejected? ValidateFact(SemanticExecutionPlan plan, SemanticValueValidator validator, SemanticFact fact, bool publicReplay)
    {
        if (!plan.Events.TryGetValue(fact.EventContract, out var eventContract) ||
            fact.Values.Length != eventContract.Properties.Length ||
            fact.Values.Select(value => value.TargetProperty).Distinct().Count() != fact.Values.Length)
        {
            return RejectedHistory($"Fact '{fact.EventContract}' does not match a known event contract and its exact property shape.");
        }

        if (ValidateFactValues(validator, fact, eventContract) is { } rejection)
        {
            return rejection;
        }

        validator.ValidateVariant(fact.Destination);
        var routedType = fact.Route is null ? null : plan.RoutedFactSourceType(fact);
        if (fact.Context is { } context)
        {
            ValidateEventSource(validator, context, routedType ?? UnroutedSourceType(plan, fact, context, publicReplay));
        }

        return null;
    }

    static SemanticRejected? ValidateFactValues(SemanticValueValidator validator, SemanticFact fact, SemanticEventContract eventContract)
    {
        var values = fact.Values.ToDictionary(value => value.TargetProperty);
        foreach (var property in eventContract.Properties)
        {
            if (!values.TryGetValue(property.Id, out var value))
            {
                return RejectedHistory($"Fact '{fact.EventContract}' is missing event property '{property.Name}'.");
            }

            validator.Validate(value.Value, property.Type, $"event property '{property.Name}'");
        }

        return null;
    }

    // Routed occurrences are typed by their source, independently of event producers. Legacy v1
    // specification events keep their authored source type; foreign events have opaque text identities.
    static SemanticTypeReference UnroutedSourceType(SemanticExecutionPlan plan, SemanticFact fact, SemanticEventContext context, bool publicReplay)
    {
        if (plan.Model.SemanticVersion == SemanticVersion.V1 && !publicReplay)
        {
            return context.EventSource.Type;
        }

        return plan.Events[fact.EventContract].Origin is not null
            ? SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)
            : SemanticModelValidator.DeclaredEventSourceType(plan.Commands.Values, plan.Reactions, plan.Captures.Values, plan.Projections.Values, fact.EventContract);
    }

    static void ValidateEventSource(SemanticValueValidator validator, SemanticEventContext context, SemanticTypeReference requiredType)
    {
        if (context.EventSource.Type != requiredType)
        {
            throw new InvalidSemanticContract("A specification event source must have the required scalar destination type.");
        }

        validator.Validate(context.EventSource.Value, requiredType, "event source identity");
    }

    static SemanticReducer? ReachedByHistory(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts) =>
        plan.Model.Application.Modules.SelectMany(module => Reducers(module.Features))
            .FirstOrDefault(candidate =>
            {
                var matchingFacts = facts.Where(fact => candidate.From?.Matches(plan.Model.Application, fact.Route) != false).ToArray();
                var observedEvents = matchingFacts.Select(fact => fact.EventContract).ToHashSet();

                return candidate.Transitions.Any(transition => observedEvents.Contains(transition.EventContract)) ||
                    (matchingFacts.Length > 0 && plan.Projections.Values.Any(projection => projection.ReadModel == candidate.ReadModel &&
                        projection.GetAffectedInstances().Any(affected => affected.EventContract is null || observedEvents.Contains(affected.EventContract.Value))));
            });
}

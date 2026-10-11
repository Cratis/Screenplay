// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Applies facts to read-model instances through every projection of a plan.
/// </summary>
/// <param name="plan">The capability-admitted plan.</param>
/// <param name="validator">The value validator for the plan's concepts and types.</param>
/// <param name="instances">The read-model instances, updated in place.</param>
/// <param name="observed">The facts observed so far.</param>
sealed class SemanticFactProjection(
    SemanticExecutionPlan plan,
    SemanticValueValidator validator,
    List<SemanticReadModelInstance> instances,
    List<SemanticFact> observed)
{
    /// <summary>
    /// Projects one fact through every projection.
    /// </summary>
    /// <param name="fact">The fact.</param>
    /// <returns>The failure, or <see langword="null"/> when the fact projected.</returns>
    public string? Apply(SemanticFact fact)
    {
        foreach (var projection in plan.Projections.Values.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal))
        {
            // An event-target projection publishes its state as an event; it builds no read-model instance.
            if (projection.Target == SemanticProjectionTargetKind.Event)
            {
                continue;
            }

            // A scoped projection runs through the reference semantics of every Chronicle projection block.
            var failure = projection.Scope is not null ? ApplyScoped(projection, fact) : ApplyTransitions(projection, fact);
            if (failure is not null)
            {
                return failure;
            }
        }

        return null;
    }

    string? ApplyScoped(SemanticProjection projection, SemanticFact fact)
    {
        try
        {
            return new SemanticScopedProjection(plan, projection, validator, observed).Apply(instances, fact);
        }
        catch (OverflowException) when (plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V6))
        {
            return $"Projection '{projection.Name}' arithmetic exceeds the reference numeric range.";
        }
    }

    string? ApplyTransitions(SemanticProjection projection, SemanticFact fact)
    {
        foreach (var transition in projection.Transitions.Where(_ => _.EventContract == fact.EventContract))
        {
            if (ApplyTransition(projection, transition, fact) is { } failure)
            {
                return failure;
            }
        }

        return null;
    }

    string? ApplyTransition(SemanticProjection projection, SemanticProjectionTransition transition, SemanticFact fact)
    {
        var eventValues = fact.Values.ToDictionary(_ => _.TargetProperty, _ => _.Value);
        var key = SemanticEvaluator.Evaluate(transition.AffectedInstance.Key, SemanticExpressionRootKind.Event, eventValues);
        var existing = instances.SingleOrDefault(instance =>
            instance.ReadModel == projection.ReadModel && SemanticValueRules.AreEqual(instance.Key, key));
        var state = existing?.Values.ToDictionary(_ => _.TargetProperty, _ => _.Value) ?? [];
        foreach (var mapping in transition.Mappings)
        {
            state[mapping.TargetProperty] = SemanticEvaluator.Evaluate(mapping.Source, SemanticExpressionRootKind.Event, eventValues);
        }

        var readModel = plan.ReadModels[projection.ReadModel];
        if (CompleteState(projection, readModel, state) is { } stateFailure)
        {
            return stateFailure;
        }

        var identifier = readModel.Properties.Single(_ => _.IsIdentifier);
        if (!SemanticValueRules.AreEqual(state[identifier.Id], key))
        {
            return $"Projection '{projection.Name}' affected key disagrees with read-model identifier '{identifier.Name}'.";
        }

        if (existing is not null)
        {
            instances.Remove(existing);
        }

        instances.Add(new(
            readModel.Id,
            key,
            [.. readModel.Properties.Select(property => new SemanticPropertyValue(property.Id, state[property.Id]))]));
        return null;
    }

    string? CompleteState(SemanticProjection projection, SemanticReadModel readModel, Dictionary<SemanticId, SemanticValue> state)
    {
        foreach (var property in readModel.Properties)
        {
            if (!state.TryGetValue(property.Id, out var value))
            {
                if (!property.Type.IsOptional)
                {
                    return $"Projection '{projection.Name}' did not establish required read-model property '{property.Name}'.";
                }

                value = SemanticValue.Null;
                state[property.Id] = value;
            }

            if (ValidatePropertyValue(property, value) is { } failure)
            {
                return failure;
            }
        }

        return null;
    }

    string? ValidatePropertyValue(SemanticProperty property, SemanticValue value)
    {
        try
        {
            validator.Validate(value, property.Type, $"read-model property '{property.Name}'");
            return null;
        }
        catch (InvalidSemanticContract exception)
        {
            return exception.Message;
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Folds private facts into the public event of an outbound translation (decision 0050).
/// </summary>
/// <remarks>
/// A projection that targets an event holds one folded state per target key, the event source identity. The published event is that
/// state: after each new fact the fold appends one new instance of the public event when the state is complete and differs from the
/// last published instance for its key. Redelivering the same fact leaves the state unchanged and so publishes nothing; a changed
/// mapping publishes new instances and never rewrites earlier ones. Only <c>set</c> and <c>clear</c> of a root property are folded;
/// anything else is a plan issue, so the evaluator fails closed rather than guess.
/// </remarks>
static class SemanticEventPublication
{
    internal static IEnumerable<SemanticPlanIssue> IssuesFor(SemanticProjection projection)
    {
        if (projection.Scope is not { } scope)
        {
            yield break;
        }

        foreach (var mapping in scope.From.SelectMany(from => from.Mappings).Concat(scope.Every?.Mappings ?? []))
        {
            if (mapping.Target.Length != 1 || mapping.Operation is not (SemanticProjectionOperation.Set or SemanticProjectionOperation.Clear) ||
                mapping.Source is not (null or SemanticProjectionLiteral or SemanticProjectionEventProperty or SemanticProjectionEventSourceIdentity or SemanticProjectionEventContextValue { Path: "eventSourceId" }))
            {
                yield return new(
                    projection.Id,
                    SemanticPlanIssueKind.UnsupportedProjectionBlock,
                    $"Projection '{projection.Name}' publishes an event; only 'set' and 'clear' of an event property from a literal or an event property are folded by the reference evaluator.");
                yield break;
            }
        }
    }

    internal static bool TryPublish(
        SemanticExecutionPlan plan,
        ImmutableArray<SemanticFact> history,
        ImmutableArray<SemanticFact> added,
        out ImmutableArray<SemanticFact> published,
        out string? failure)
    {
        var results = ImmutableArray.CreateBuilder<SemanticFact>();
        failure = null;
        published = [];
        var projections = plan.Projections.Values.Where(projection => projection.Target == SemanticProjectionTargetKind.Event)
            .OrderBy(projection => projection.Id.ToString(), StringComparer.Ordinal).ToArray();
        if (projections.Length == 0)
        {
            return true;
        }

        var validator = new SemanticValueValidator(
            plan.Model.Application.Concepts.ToDictionary(concept => concept.Id),
            plan.Model.Application.Types.ToDictionary(type => type.Id));
        foreach (var projection in projections)
        {
            var target = plan.Events[projection.ReadModel];
            var states = new List<(SemanticValue Key, Dictionary<SemanticId, SemanticValue> State)>();
            var last = new List<(SemanticValue Key, ImmutableArray<SemanticPropertyValue> Values)>();
            foreach (var fact in history.Concat(results).Where(fact => fact.EventContract == target.Id))
            {
                SetLast(last, fact.Destination, fact.Values);
            }

            foreach (var (fact, isNew) in history.Select(fact => (fact, false)).Concat(added.Select(fact => (fact, true))))
            {
                var from = projection.Scope!.From.Where(candidate => candidate.EventContract == fact.EventContract).ToArray();
                if (from.Length == 0 && projection.Scope.Every is null)
                {
                    continue;
                }

                var state = StateFor(states, fact.Destination);
                foreach (var mapping in from.SelectMany(candidate => candidate.Mappings).Concat(projection.Scope.Every?.Mappings ?? []))
                {
                    var property = mapping.Target[0];
                    if (mapping.Operation == SemanticProjectionOperation.Clear)
                    {
                        state.Remove(property);
                        continue;
                    }

                    state[property] = Evaluate(mapping.Source!, fact);
                }

                if (!isNew)
                {
                    continue;
                }

                var values = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
                var complete = true;
                foreach (var property in target.Properties)
                {
                    if (!state.TryGetValue(property.Id, out var value) || (value is SemanticNullValue && !property.Type.IsOptional))
                    {
                        if (!property.Type.IsOptional)
                        {
                            complete = false;
                            break;
                        }

                        value = SemanticValue.Null;
                    }

                    try
                    {
                        validator.Validate(value, property.Type, $"event property '{property.Name}'");
                    }
                    catch (InvalidSemanticContract exception)
                    {
                        failure = $"Projection '{projection.Name}' cannot publish '{target.Name}': {exception.Message}";
                        return false;
                    }

                    values.Add(new(property.Id, value));
                }

                if (!complete)
                {
                    continue;
                }

                var previous = last.Find(entry => SemanticValueRules.AreEqual(entry.Key, fact.Destination));
                if (!previous.Values.IsDefault && SameValues(previous.Values, values))
                {
                    continue;
                }

                var emitted = new SemanticFact(target.Id, fact.Destination, values.ToImmutable())
                {
                    Context = fact.Context,
                    Tags = target.Tags,
                    Occurred = fact.Occurred,
                    ReactionOrigin = fact.ReactionOrigin,
                    Route = fact.Route
                };
                results.Add(emitted);
                SetLast(last, fact.Destination, emitted.Values);
            }
        }

        published = results.ToImmutable();
        return true;
    }

    static Dictionary<SemanticId, SemanticValue> StateFor(List<(SemanticValue Key, Dictionary<SemanticId, SemanticValue> State)> states, SemanticValue key)
    {
        var index = states.FindIndex(entry => SemanticValueRules.AreEqual(entry.Key, key));
        if (index >= 0)
        {
            return states[index].State;
        }

        var state = new Dictionary<SemanticId, SemanticValue>();
        states.Add((key, state));
        return state;
    }

    static void SetLast(List<(SemanticValue Key, ImmutableArray<SemanticPropertyValue> Values)> last, SemanticValue key, ImmutableArray<SemanticPropertyValue> values)
    {
        last.RemoveAll(entry => SemanticValueRules.AreEqual(entry.Key, key));
        last.Add((key, values));
    }

    static bool SameValues(ImmutableArray<SemanticPropertyValue> left, IEnumerable<SemanticPropertyValue> right)
    {
        var other = right.ToDictionary(value => value.TargetProperty, value => value.Value);
        return left.Length == other.Count && left.All(value => other.TryGetValue(value.TargetProperty, out var match) && SemanticValueRules.AreEqual(value.Value, match));
    }

    static SemanticValue Evaluate(SemanticProjectionValue value, SemanticFact fact)
    {
        switch (value)
        {
            case SemanticProjectionLiteral literal:
                return literal.Value;
            case SemanticProjectionEventProperty property:
                var current = fact.Values.FirstOrDefault(candidate => candidate.TargetProperty == property.Path[0])?.Value ?? SemanticValue.Null;
                foreach (var segment in property.Path[1..])
                {
                    current = current is SemanticCompositeValue composite
                        ? composite.Properties.FirstOrDefault(candidate => candidate.TargetProperty == segment)?.Value ?? SemanticValue.Null
                        : SemanticValue.Null;
                }

                return current;
            default:
                return fact.Context?.EventSource.Value ?? fact.Destination;
        }
    }
}

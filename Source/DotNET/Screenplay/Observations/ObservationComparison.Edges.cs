// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations;

internal sealed partial class ObservationComparisonRun
{
    void CompareEdges()
    {
        foreach (var slice in _index.ModelSlices)
        {
            foreach (var command in slice.Commands)
            {
                CompareProductions(command.Id, command.Produces, command.Handler is null ? null : ObservationUnknownReason.DescribedHandler);
            }

            foreach (var reaction in slice.Reactions)
            {
                foreach (var trigger in reaction.Triggers)
                {
                    var reason = trigger switch
                    {
                        { RequirementId: not null } => ObservationUnknownReason.OpaqueReaction,
                        { Kind: not SemanticReactionTriggerKind.Event } => ObservationUnknownReason.NonEventTrigger,
                        { Where: not null } => ObservationUnknownReason.ConditionalProduction,
                        _ => (ObservationUnknownReason?)null
                    };
                    if (reason is not null)
                    {
                        Add(ObservationFindingKind.NotEstablished, ObservationOutcome.Unknown, "The reaction trigger or body cannot establish unconditional event production.", reaction.Id, reason: reason);
                    }

                    CompareProductions(reaction.Id, trigger.Produces, reason);
                }
            }
        }

        foreach (var edge in snapshot.Edges.Where(value => value.Events > 0))
        {
            CompareObservedEdge(edge);
        }
    }

    void CompareProductions(SemanticId id, IEnumerable<SemanticProducedEvent> productions, ObservationUnknownReason? limitation)
    {
        var producer = map.Producers.GetValueOrDefault(id);
        foreach (var production in productions)
        {
            var type = CurrentType(production.EventContract);
            var count = EdgeCount(producer, type);
            var reason = MissingEdgeReason(id, production, limitation, producer, type);
            var foreign = Foreign(production.EventContract);
            var outcome = reason is not null ? ObservationOutcome.Unknown : ObservationOutcome.Informational;
            if (foreign) outcome = ObservationOutcome.Excluded;
            if (count == 0 && !foreign)
            {
                var ran = producer is not null && snapshot.Edges.Any(value => value.Producer == producer && value.Events > 0);
                var kind = reason is not null || ran ? ObservationFindingKind.DeclaredEdgeNotObserved : ObservationFindingKind.NoMatchingAppendsWithinCoverage;
                outcome = reason is null && ran ? ObservationOutcome.Drift : outcome;
                var statement = kind == ObservationFindingKind.NoMatchingAppendsWithinCoverage ? "No attributed appends establish that this producer ran within coverage." : "No appends of the declared production were attributed to this producer within coverage.";
                Add(kind, outcome, statement, id, type, producer, count: 0, reason: reason);
            }

            if (!_edges.TryGetValue(id, out var values))
            {
                _edges[id] = values = [];
            }

            values.Add(new(production.EventContract, count, outcome, reason));
        }
    }

    ObservationUnknownReason? MissingEdgeReason(SemanticId id, SemanticProducedEvent production, ObservationUnknownReason? limitation, ObservedProducer? producer, ObservedEventType? type)
    {
        if (AmbiguousProducer(id) || AmbiguousEvent(production.EventContract)) return ObservationUnknownReason.AmbiguousIdentity;
        if (producer is null || type is null) return ObservationUnknownReason.UnmappedConstruct;
        if (EdgeCount(producer, type) > 0) return null;
        if (!snapshot.Coverage.Complete) return ObservationUnknownReason.PartialCoverage;
        if (production.When is not null || production.Condition is not null) return ObservationUnknownReason.ConditionalProduction;
        if (limitation is not null) return limitation;
        if (ReactionBehind(id)) return ObservationUnknownReason.ConsumerBehind;
        if (HasUnattributed(type)) return ObservationUnknownReason.UnattributedEvents;

        return null;
    }

    void CompareObservedEdge(ObservedEdge edge)
    {
        var matches = map.Producers.Where(value => value.Value == edge.Producer && _index.Owner(value.Key) is not null).Select(value => value.Key).ToArray();
        if (matches.Length != 1)
        {
            Add(ObservationFindingKind.NotEstablished, ObservationOutcome.Unknown, "The observed producer has no unambiguous model mapping.", type: edge.Type, producer: edge.Producer, count: edge.Events, reason: matches.Length == 0 ? ObservationUnknownReason.UnmappedProducer : ObservationUnknownReason.AmbiguousIdentity);
            return;
        }

        var id = matches[0];
        var slice = _index.ModelSlices.Single(value => value.Id == _index.Owner(id));
        var command = slice.Commands.FirstOrDefault(value => value.Id == id);
        var reaction = slice.Reactions.FirstOrDefault(value => value.Id == id);
        var productions = command?.Produces.AsEnumerable() ?? reaction?.Triggers.SelectMany(value => value.Produces) ?? [];
        if (productions.Any(value => CurrentType(value.EventContract)?.TypeId == edge.Type.TypeId)) return;
        if (_index.Events.Values.Any(value => Foreign(value.Id) && CurrentType(value.Id)?.TypeId == edge.Type.TypeId)) return;
        var opaque = reaction?.Triggers.Any(value => value.RequirementId is not null) == true;
        Add(ObservationFindingKind.UndeclaredEdgeObserved, opaque ? ObservationOutcome.Unknown : ObservationOutcome.Drift, "The observed producer appended an event outside its declared productions.", id, edge.Type, edge.Producer, count: edge.Events, reason: opaque ? ObservationUnknownReason.OpaqueReaction : null);
    }
}

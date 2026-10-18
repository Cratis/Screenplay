// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations;

internal sealed partial class ObservationComparisonRun
{
    static int OutcomeRank(ObservationOutcome outcome) => outcome switch
    {
        ObservationOutcome.Drift => 0,
        ObservationOutcome.Unknown => 1,
        _ => 2
    };

    static IEnumerable<SemanticId> ProjectionEvents(SemanticProjection projection) => projection.Scope is { } scope
        ? ScopeEvents(scope).Distinct() : projection.Transitions.Select(value => value.EventContract).Distinct();

    static IEnumerable<SemanticId> ScopeEvents(SemanticProjectionScope scope) => scope.From.Select(value => value.EventContract)
        .Concat(scope.Joins.Select(value => value.EventContract)).Concat(scope.Removals.Select(value => value.EventContract))
        .Concat(scope.JoinRemovals.Select(value => value.EventContract)).Concat(scope.Children.SelectMany(value => ScopeEvents(value.Scope)))
        .Concat(scope.Nested.SelectMany(value => ScopeEvents(value.Scope)));

    static SliceConsumerObservation DeferredConsumer(SemanticId construct, SemanticId source) =>
        new(construct, source, [], ObservationOutcome.Unknown, ObservationUnknownReason.ConsumerKindNotCompared);

    ImmutableArray<SliceObservation> SliceViews() => [.. _index.ModelSlices.Select(SliceView)];

    SliceObservation SliceView(SemanticSlice slice) => new(
        slice.Id,
        _index.Address(slice.Id),
        slice.Kind,
        [.. slice.Events.Select(EventView)],
        [.. slice.Commands.Select(value => ProducerView(value.Id, ObservedProducerKind.Command)).Concat(slice.Reactions.Select(value => ProducerView(value.Id, ObservedProducerKind.Reaction)))],
        [.. slice.Reactions.SelectMany(value => _consumers.GetValueOrDefault(value.Id) ?? [])
            .Concat(slice.Projections.SelectMany(value => ProjectionEvents(value).Select(source => DeferredConsumer(value.Id, source))))
            .Concat(slice.Reducers.SelectMany(value => value.Transitions.Select(transition => transition.EventContract).Distinct().Select(source => DeferredConsumer(value.ReadModel, source))))]);

    SliceEventObservation EventView(SemanticEventContract contract)
    {
        var type = CurrentType(contract.Id);

        return new(contract.Id, type?.TypeId, [.. snapshot.Appends.Where(value => value.Type.TypeId == type?.TypeId)], type is not null && snapshot.RegisteredEventTypes.Contains(type));
    }

    SliceProducerObservation ProducerView(SemanticId id, ObservedProducerKind kind) => new(id, kind, [.. (_edges.GetValueOrDefault(id) ?? []).GroupBy(value => value.Event).Select(group => group.OrderBy(value => OutcomeRank(value.Outcome)).First())]);
}

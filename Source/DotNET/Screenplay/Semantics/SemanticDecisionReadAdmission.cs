// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static class SemanticDecisionReadAdmission
{
    internal static SemanticDecisionReadRefusal? Key(SemanticProperty key, IReadOnlyDictionary<SemanticId, SemanticConcept> concepts)
    {
        if (key.IsGenerated) return SemanticDecisionReadRefusal.GeneratedKey;
        if (key.Type.IsOptional) return SemanticDecisionReadRefusal.OptionalKey;
        if (key.Type.IsCollection) return SemanticDecisionReadRefusal.CollectionKey;
        var concept = key.Type.Kind == SemanticTypeReferenceKind.Concept ? concepts.GetValueOrDefault(key.Type.Target) : null;
        var primitive = key.Type.Kind == SemanticTypeReferenceKind.Primitive ? key.Type.Primitive : concept?.Primitive;
        var enumeration = concept is { Values.IsEmpty: false };
        return !enumeration && primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid ? null : SemanticDecisionReadRefusal.KeyType;
    }

    internal static bool Routed(
        bool commandRoute,
        bool namedSequence,
        IEnumerable<SemanticProjection> projections,
        IEnumerable<(SemanticId EventContract, bool Routed)> producers)
    {
        var handled = projections.SelectMany(projection => projection.Scope is { } scope
            ? scope.From.Select(from => from.EventContract).Concat(scope.Removals.Select(removal => removal.EventContract))
            : projection.Transitions.Select(transition => transition.EventContract)).ToHashSet();
        return commandRoute || namedSequence || producers.Any(producer => producer.Routed && handled.Contains(producer.EventContract));
    }

    // Capture appends have no output-route member in the admitted ESM; their source filter is not an output route.
    internal static IEnumerable<(SemanticId EventContract, bool Routed)> Producers(IEnumerable<SemanticSlice> slices) =>
        slices.SelectMany(slice => ProducerItems(slice.Commands).SelectMany(command => ProducerItems(command.Produces).Select(produced =>
                (produced.EventContract, command.Route is not null || produced.Route is not null)))
            .Concat(ProducerItems(slice.Reactions).SelectMany(reaction => ProducerItems(reaction.Triggers)).SelectMany(trigger =>
                ProducerItems(trigger.Produces).Concat(ProducerItems(trigger.Invokes).SelectMany(invocation => ProducerItems(invocation.OnRefused)).SelectMany(branch => ProducerItems(branch.Produces))))
                .Select(produced => (produced.EventContract, produced.Route is not null)))
            .Concat(ProducerItems(slice.Captures).SelectMany(capture => ProducerItems(capture.Appends)
                    .Concat(ProducerItems(capture.Children).SelectMany(child => ProducerItems(child.Appends)))
                    .Concat(ProducerItems(capture.Nested).SelectMany(nested => ProducerItems(nested.Appends))))
                .Select(append => (append.EventContract, false))));

    internal static SemanticDecisionReadRefusal? View(IEnumerable<SemanticProjection> projections, bool reducer, bool routed)
    {
        if (reducer) return SemanticDecisionReadRefusal.Reducer;
        var builders = projections.ToArray();
        var handled = new HashSet<SemanticId>();
        var removes = false;
        foreach (var projection in builders)
        {
            if (projection.Scope is not { } scope)
            {
                if (!projection.Transitions.IsEmpty) return SemanticDecisionReadRefusal.PayloadKey;
                continue;
            }
            if (!scope.Joins.IsEmpty || !scope.JoinRemovals.IsEmpty) return SemanticDecisionReadRefusal.Join;
            if (!scope.Children.IsEmpty || !scope.Nested.IsEmpty || scope.From.Any(from => from.ParentKey is not null) || scope.Removals.Any(removal => removal.ParentKey is not null)) return SemanticDecisionReadRefusal.Children;
            if (scope.Every is not null) return SemanticDecisionReadRefusal.OpenSubscription;
            foreach (var key in scope.From.Select(from => from.Key).Concat(scope.Removals.Select(removal => removal.Key)))
            {
                if (key is SemanticProjectionCompositeKey or SemanticProjectionPartsKey) return SemanticDecisionReadRefusal.ByParts;
                if (key is not SemanticProjectionValueKey { Value: SemanticProjectionEventSourceIdentity }) return SemanticDecisionReadRefusal.PayloadKey;
            }
            handled.UnionWith(scope.From.Select(from => from.EventContract));
            handled.UnionWith(scope.Removals.Select(removal => removal.EventContract));
            removes |= !scope.Removals.IsEmpty;
        }
        if (routed) return SemanticDecisionReadRefusal.NamedSourceOrStream;
        if (handled.Count == 0) return SemanticDecisionReadRefusal.EmptyHandledSet;
        return removes ? null : SemanticDecisionReadRefusal.NoRemoval;
    }

    static ImmutableArray<T> ProducerItems<T>(ImmutableArray<T> items)
        where T : class =>
        !items.IsDefault && items.All(item => item is not null)
            ? items
            : throw new InvalidSemanticContract("Decision read producer collections must be initialized and contain no null entries.");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static class SemanticDecisionReadAdmission
{
    internal static SemanticDecisionReadRefusal? Key(SemanticProperty key, SemanticPrimitiveType primitive, bool enumeration = false)
    {
        if (key.IsGenerated) return SemanticDecisionReadRefusal.GeneratedKey;
        if (key.Type.IsOptional) return SemanticDecisionReadRefusal.OptionalKey;
        if (key.Type.IsCollection) return SemanticDecisionReadRefusal.CollectionKey;
        return !enumeration && primitive is SemanticPrimitiveType.Text or SemanticPrimitiveType.Uuid ? null : SemanticDecisionReadRefusal.KeyType;
    }

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
                if (key is SemanticProjectionCompositeKey) return SemanticDecisionReadRefusal.ByParts;
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
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>The projection block whose affected instances are described.</summary>
public enum SemanticAffectedProjectionBlock
{
    /// <summary>A transition which creates or updates one keyed instance.</summary>
    From,

    /// <summary>A transition which updates existing instances by a joined property.</summary>
    Join,

    /// <summary>A subscription to events not named by another block.</summary>
    All,

    /// <summary>A keyed removal.</summary>
    Removal,

    /// <summary>A removal of matching children across parents.</summary>
    JoinRemoval
}

/// <summary>How existing read-model instances are located for an event and projection block.</summary>
public enum SemanticAffectedProjectionMatch
{
    /// <summary>One instance addressed by a key (which may be deferred until its parent exists).</summary>
    OneByKey,

    /// <summary>One instance addressed by the occurrence's event source identity.</summary>
    OneByEventSource,

    /// <summary>Every existing instance whose property equals the occurrence's event source identity.</summary>
    ManyByPropertyAndEventSource,

    /// <summary>Matching children across parents; never creates a parent.</summary>
    ManyChildrenByKey
}

/// <summary>
/// A read-only relationship derived from a projection, not authored or included in canonical ESM bytes.
/// </summary>
/// <param name="EventContract">The event type, or null for the open subscription of a root <c>all</c> block.</param>
/// <param name="Block">The event block.</param>
/// <param name="Match">The instance-selection shape.</param>
/// <param name="Path">Properties traversed from the read-model root through child collections or nested objects.</param>
/// <param name="Key">The key for scoped blocks, if one is used.</param>
/// <param name="FlatKey">The expression for a flat transition, if one is used.</param>
/// <param name="Property">The property compared in a join; null for other blocks.</param>
/// <param name="ParentKey">The parent key for a child <c>from</c> or removal; null elsewhere.</param>
/// <remarks>For a root join, Chronicle compares <paramref name="Property"/> with the event source identity,
/// even when the variant join carries an explicit key (Cratis/Chronicle#4165).</remarks>
public sealed record SemanticAffectedProjectionInstance(
    SemanticId? EventContract,
    SemanticAffectedProjectionBlock Block,
    SemanticAffectedProjectionMatch Match,
    ImmutableArray<SemanticId> Path,
    SemanticProjectionKey? Key,
    SemanticExpression? FlatKey,
    SemanticId? Property,
    SemanticProjectionKey? ParentKey);

/// <summary>Derives affected-instance relationships without changing a projection or its canonical representation.</summary>
public static class SemanticAffectedProjectionInstances
{
    /// <summary>Returns one relationship for each event/block, including the wildcard of a root <c>all</c> subscription.</summary>
    public static ImmutableArray<SemanticAffectedProjectionInstance> GetAffectedInstances(this SemanticProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        var result = ImmutableArray.CreateBuilder<SemanticAffectedProjectionInstance>();
        if (projection.Scope is null)
        {
            foreach (var transition in projection.Transitions)
            {
                result.Add(new(transition.EventContract, SemanticAffectedProjectionBlock.From, SemanticAffectedProjectionMatch.OneByKey, [], null, transition.AffectedInstance.Key, null, null));
            }
        }
        else
        {
            Collect(projection.Scope, [], false, result);
        }

        return result.ToImmutable();
    }

    static void Collect(
        SemanticProjectionScope scope,
        ImmutableArray<SemanticId> path,
        bool child,
        ImmutableArray<SemanticAffectedProjectionInstance>.Builder result)
    {
        foreach (var from in scope.From)
        {
            result.Add(new(from.EventContract, SemanticAffectedProjectionBlock.From, SemanticAffectedProjectionMatch.OneByKey, path, from.Key, null, null, from.ParentKey));
        }

        foreach (var join in scope.Joins)
        {
            result.Add(new(
                join.EventContract,
                SemanticAffectedProjectionBlock.Join,
                child ? SemanticAffectedProjectionMatch.ManyChildrenByKey : SemanticAffectedProjectionMatch.ManyByPropertyAndEventSource,
                path,
                child ? join.Key ?? SemanticProjectionKey.EventSourceIdentity : null,
                null,
                join.On,
                null));
        }

        if (path.IsEmpty && scope.Every is { SubscribesToAllEvents: true })
        {
            result.Add(new(null, SemanticAffectedProjectionBlock.All, SemanticAffectedProjectionMatch.OneByEventSource, path, SemanticProjectionKey.EventSourceIdentity, null, null, null));
        }

        foreach (var removal in scope.Removals)
        {
            result.Add(new(removal.EventContract, SemanticAffectedProjectionBlock.Removal, SemanticAffectedProjectionMatch.OneByKey, path, removal.Key, null, null, removal.ParentKey));
        }

        foreach (var removal in scope.JoinRemovals)
        {
            // The root shape has no verified runtime meaning and is rejected by the reference execution plan.
            if (path.IsEmpty)
            {
                throw new InvalidSemanticContract("A projection-level remove via join has no verified affected-instance meaning.");
            }

            result.Add(new(removal.EventContract, SemanticAffectedProjectionBlock.JoinRemoval, SemanticAffectedProjectionMatch.ManyChildrenByKey, path, removal.Key, null, null, null));
        }

        foreach (var children in scope.Children)
        {
            Collect(children.Scope, path.Add(children.Property), true, result);
        }

        foreach (var nested in scope.Nested)
        {
            if (!nested.Scope.Joins.IsEmpty || !nested.Scope.Children.IsEmpty || !nested.Scope.JoinRemovals.IsEmpty)
            {
                throw new InvalidSemanticContract("A join, children block or remove via join inside a nested object has no verified affected-instance meaning.");
            }

            Collect(nested.Scope, path.Add(nested.Property), child, result);
        }
    }
}

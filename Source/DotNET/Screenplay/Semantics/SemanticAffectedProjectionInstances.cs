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

    /// <summary>A subscription to every event type without a projection-level <c>from</c>; other blocks keep their own key resolution.</summary>
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
    ManyChildrenByKey,

    /// <summary>The runtime does not establish an affected-instance relationship for this block.</summary>
    Unverified
}

/// <summary>A read-only relationship derived from a projection, not authored or included in canonical ESM bytes.</summary>
/// <remarks>For a root join, Chronicle compares <see cref="Property"/> with the event source identity,
/// even when the variant join carries an explicit key (Cratis/Chronicle#4165).</remarks>
public sealed record SemanticAffectedProjectionInstance
{
    /// <summary>The event type, or null for the open subscription of a root <c>all</c> block.</summary>
    public required SemanticId? EventContract { get; init; }

    /// <summary>The event block.</summary>
    public required SemanticAffectedProjectionBlock Block { get; init; }

    /// <summary>The instance-selection shape.</summary>
    public required SemanticAffectedProjectionMatch Match { get; init; }

    /// <summary>Properties traversed from the read-model root through child collections or nested objects.</summary>
    public required ImmutableArray<SemanticId> Path { get; init; }

    /// <summary>The key for scoped blocks, if one is used.</summary>
    public SemanticProjectionKey? Key { get; init; }

    /// <summary>The expression for a flat transition, if one is used.</summary>
    public SemanticExpression? FlatKey { get; init; }

    /// <summary>The property compared in a join; null for other blocks.</summary>
    public SemanticId? Property { get; init; }

    /// <summary>The parent key for a child <c>from</c> or removal; null elsewhere.</summary>
    public SemanticProjectionKey? ParentKey { get; init; }
}

/// <summary>Derives affected-instance relationships without changing a projection or its canonical representation.</summary>
public static class SemanticAffectedProjectionInstances
{
    /// <summary>Returns one relationship for each effective event/block, including the wildcard of a root <c>all</c> subscription.</summary>
    public static ImmutableArray<SemanticAffectedProjectionInstance> GetAffectedInstances(this SemanticProjection projection)
    {
        ArgumentNullException.ThrowIfNull(projection);
        return CollectAffected(projection, null);
    }

    /// <summary>Returns relationships with fallback child identities resolved against the read-model schema.</summary>
    public static ImmutableArray<SemanticAffectedProjectionInstance> GetAffectedInstances(this SemanticProjection projection, SemanticApplication application)
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(application);
        return CollectAffected(projection, application);
    }

    static ImmutableArray<SemanticAffectedProjectionInstance> CollectAffected(SemanticProjection projection, SemanticApplication? application)
    {
        var result = ImmutableArray.CreateBuilder<SemanticAffectedProjectionInstance>();
        if (projection.Scope is null)
        {
            foreach (var transition in projection.Transitions)
            {
                result.Add(new()
                {
                    EventContract = transition.EventContract, Block = SemanticAffectedProjectionBlock.From,
                    Match = SemanticAffectedProjectionMatch.OneByKey, Path = [], FlatKey = transition.AffectedInstance.Key
                });
            }
        }
        else
        {
            var readModel = application?.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
                .SelectMany(slice => slice.ReadModels).SingleOrDefault(_ => _.Id == projection.ReadModel);
            var identifierName = readModel?.Properties.Single(_ => _.IsIdentifier).Name;
            Collect(projection.Scope, [], null, false, result, application, readModel?.Properties, identifierName);
        }

        return result.ToImmutable();
    }

    static void Collect(
        SemanticProjectionScope scope,
        ImmutableArray<SemanticId> path,
        SemanticId? childIdentity,
        bool child,
        ImmutableArray<SemanticAffectedProjectionInstance>.Builder result,
        SemanticApplication? application,
        ImmutableArray<SemanticProperty>? properties,
        string? identifierName)
    {
        foreach (var from in scope.From)
        {
            result.Add(new()
            {
                EventContract = from.EventContract,
                Block = SemanticAffectedProjectionBlock.From,
                Match = SemanticAffectedProjectionMatch.OneByKey,
                Path = path,
                Key = from.Key,
                ParentKey = from.ParentKey
            });
        }

        foreach (var join in scope.Joins)
        {
            var match = child ? SemanticAffectedProjectionMatch.ManyChildrenByKey : SemanticAffectedProjectionMatch.ManyByPropertyAndEventSource;
            if (child && childIdentity is null)
            {
                match = SemanticAffectedProjectionMatch.Unverified;
            }
            result.Add(new()
            {
                EventContract = join.EventContract,
                Block = SemanticAffectedProjectionBlock.Join,
                Match = match,
                Path = path,
                Key = child ? join.Key ?? SemanticProjectionKey.EventSourceIdentity : null,
                Property = child ? childIdentity : join.On
            });
        }

        if (path.IsEmpty && scope.Every is { SubscribesToAllEvents: true })
        {
            // ProjectionFactory.cs:633-643 excludes only root from types; other blocks retain their own resolvers.
            result.Add(new()
            {
                EventContract = null,
                Block = SemanticAffectedProjectionBlock.All,
                Match = SemanticAffectedProjectionMatch.OneByEventSource,
                Path = path,
                Key = SemanticProjectionKey.EventSourceIdentity
            });
        }

        foreach (var removal in scope.Removals)
        {
            result.Add(new()
            {
                EventContract = removal.EventContract,
                Block = SemanticAffectedProjectionBlock.Removal,
                Match = SemanticAffectedProjectionMatch.OneByKey,
                Path = path,
                Key = removal.Key,
                ParentKey = removal.ParentKey
            });
        }

        foreach (var removal in scope.JoinRemovals)
        {
            // A root removal is wired to RemoveChildFromAll with a root (empty) children path;
            // it cannot be described as deleting root instances (ProjectionFactory.cs:297-308, Sink.cs:662-674).
            result.Add(new()
            {
                EventContract = removal.EventContract,
                Block = SemanticAffectedProjectionBlock.JoinRemoval,
                Match = path.IsEmpty || childIdentity is null ? SemanticAffectedProjectionMatch.Unverified : SemanticAffectedProjectionMatch.ManyChildrenByKey,
                Path = path,
                Key = removal.Key,
                Property = childIdentity
            });
        }

        // Children under nested objects are not subscribed by SetupNestedSubscriptions/CollectNestedEventTypes.
        // The caller skips them rather than reporting relationships Chronicle never executes.
        foreach (var children in scope.Children)
        {
            var elementType = properties?.Single(_ => _.Id == children.Property).Type.Target;
            var elementProperties = application?.Types.Single(_ => _.Id == elementType).Properties;
            SemanticId? identity = children.IdentifiedBy.IsSet ? children.IdentifiedBy : null;
            if (identity is null && identifierName is not null)
            {
                identity = elementProperties?.Single(_ => _.Name == identifierName).Id;
            }
            Collect(children.Scope, path.Add(children.Property), identity, true, result, application, elementProperties, identifierName);
        }

        foreach (var nested in scope.Nested)
        {
            CollectNested(nested.Scope, path.Add(nested.Property), result);
        }
    }

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));

    static void CollectNested(
        SemanticProjectionScope scope,
        ImmutableArray<SemanticId> path,
        ImmutableArray<SemanticAffectedProjectionInstance>.Builder result)
    {
        // Only from, removals and further nested objects are subscribed in Chronicle's nested scope.
        foreach (var from in scope.From)
        {
            result.Add(new()
            {
                EventContract = from.EventContract,
                Block = SemanticAffectedProjectionBlock.From,
                Match = SemanticAffectedProjectionMatch.OneByKey,
                Path = path,
                Key = from.Key,
                ParentKey = from.ParentKey
            });
        }
        foreach (var removal in scope.Removals)
        {
            result.Add(new()
            {
                EventContract = removal.EventContract,
                Block = SemanticAffectedProjectionBlock.Removal,
                Match = SemanticAffectedProjectionMatch.OneByKey,
                Path = path,
                Key = removal.Key,
                ParentKey = removal.ParentKey
            });
        }
        foreach (var nested in scope.Nested)
        {
            CollectNested(nested.Scope, path.Add(nested.Property), result);
        }
    }
}

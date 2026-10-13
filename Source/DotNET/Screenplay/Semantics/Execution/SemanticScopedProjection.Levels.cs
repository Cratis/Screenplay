// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Carries projection paths and original child occurrences across immutable reference worlds.
/// </summary>
internal sealed partial class SemanticScopedProjection
{
    Level ChildLevel(SemanticProjectionChildren children, SemanticProjectionScope scope, Level level)
    {
        var element = _types[level.Targets[children.Property].Type.Target];
        var elementProperties = Properties(element.Properties);
        var identity = children.IdentifiedBy.IsSet
            ? children.IdentifiedBy
            : element.Properties.Single(_ => _.Name == _readModel.Properties.Single(property => property.IsIdentifier).Name).Id;

        return new([.. level.Address, new(children.Property, identity)], [], elementProperties, [.. level.Ancestors.IsDefault ? [] : level.Ancestors, new(scope, level)]);
    }

    internal sealed record ChildStep(SemanticId Property, SemanticId IdentifiedBy);

    internal sealed record Level(
        ImmutableArray<ChildStep> Address,
        ImmutableArray<SemanticId> Nested,
        Dictionary<SemanticId, SemanticProperty> Targets,
        ImmutableArray<Ancestor> Ancestors = default);

    internal sealed record Ancestor(SemanticProjectionScope Scope, Level Level);

    internal sealed record DeferredChild(
        SemanticId Projection,
        SemanticFact Fact,
        SemanticProjectionFrom? From,
        SemanticProjectionScope Scope,
        Level Level,
        SemanticValue Identity,
        SemanticValue ParentIdentity,
        SemanticProjectionRemoval? Removal = null);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Validates the scoped projection shape, mirroring Chronicle's <c>ProjectionValidator</c> where it has a rule.
/// </summary>
internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        enum ProjectionLevelKind
        {
            Root = 0,
            Children = 1,
            Nested = 2
        }

        void ValidateProjectionScope(SemanticProjection projection, SemanticReadModel readModel)
        {
            RequireArray(projection.Transitions, nameof(projection.Transitions));
            if (!projection.Transitions.IsEmpty)
            {
                throw new InvalidSemanticContract($"Projection '{projection.Name}' carries both flat transitions and a scope.");
            }

            var identifier = IdentifierProperty(readModel);
            var level = new ProjectionLevel(Properties(readModel.Properties), identifier.Type, null, ProjectionLevelKind.Root, identifier.Name);
            ValidateScope(projection.Scope!, level);
        }

        void ValidateScope(SemanticProjectionScope scope, ProjectionLevel level)
        {
            RequireObjects(scope.From, nameof(scope.From), "projection transition");
            RequireObjects(scope.Joins, nameof(scope.Joins), "projection join");
            RequireObjects(scope.Children, nameof(scope.Children), "projection children");
            RequireObjects(scope.Nested, nameof(scope.Nested), "projection nested object");
            RequireObjects(scope.Removals, nameof(scope.Removals), "projection removal");
            RequireObjects(scope.JoinRemovals, nameof(scope.JoinRemovals), "projection join removal");

            // Chronicle's ProjectionValidator.ValidateDuplicateEvents (ProjectionValidator.cs:92-138): an event type is used once per
            // level across from, remove with and remove via join; the joins of a level have their own set.
            var levelEvents = new HashSet<SemanticId>();
            var joinEvents = new HashSet<SemanticId>();
            foreach (var from in scope.From)
            {
                RequireUniqueEvent(levelEvents, from.EventContract);
                var sources = EventProperties(from.EventContract);
                ValidateProjectionKey(from.Key, level.Identity, sources, "transition key");
                ValidateParentKey(from.ParentKey, level, sources);
                ValidateProjectionMappings(from.Mappings, level.Targets, sources);
            }

            foreach (var join in scope.Joins)
            {
                RequireUniqueEvent(joinEvents, join.EventContract);
                var sources = EventProperties(join.EventContract);
                if (!level.Targets.TryGetValue(join.On, out var on) || on.Type.IsCollection)
                {
                    throw new InvalidSemanticContract($"Projection join property '{join.On}' must be one scalar property of its level.");
                }

                if (join.Key is not null)
                {
                    ValidateProjectionKey(join.Key, on.Type, sources, "join key");
                }

                ValidateProjectionMappings(join.Mappings, level.Targets, sources);
            }

            foreach (var removal in scope.Removals)
            {
                RequireUniqueEvent(levelEvents, removal.EventContract);
                var sources = EventProperties(removal.EventContract);
                ValidateProjectionKey(removal.Key, level.Identity, sources, "removal key");
                ValidateParentKey(removal.ParentKey, level, sources);
            }

            foreach (var removal in scope.JoinRemovals)
            {
                RequireUniqueEvent(levelEvents, removal.EventContract);
                ValidateProjectionKey(removal.Key, level.Identity, EventProperties(removal.EventContract), "join removal key");
            }

            ValidateEvery(scope.Every, level);
            ValidateChildScopes(scope, level);
        }

        void ValidateChildScopes(SemanticProjectionScope scope, ProjectionLevel level)
        {
            var structural = new HashSet<SemanticId>();
            foreach (var children in scope.Children)
            {
                RejectNull(children.Scope, "projection children scope");

                // Chronicle's ProjectionValidator.ValidateChildren (ProjectionValidator.cs:217-238): the property must exist and be
                // an array with an item schema - here, a collection of one composite type.
                if (!structural.Add(children.Property) || !level.Targets.TryGetValue(children.Property, out var property) ||
                    property.Type is not { Kind: SemanticTypeReferenceKind.CompositeType, IsCollection: true } ||
                    !_types.TryGetValue(property.Type.Target, out var element))
                {
                    throw new InvalidSemanticContract($"Projection children property '{children.Property}' must be one collection of a composite type on its level.");
                }

                var elementProperties = Properties(element.Properties);
                var identity = children.IdentifiedBy.IsSet
                    ? elementProperties.GetValueOrDefault(children.IdentifiedBy)

                    // Chronicle ProjectionFactory.cs:482-484: an unset identity uses the read model key property's name.
                    : element.Properties.FirstOrDefault(_ => _.Name == level.IdentifierName);
                if (identity?.Type.IsCollection != false)
                {
                    throw new InvalidSemanticContract($"Projection children '{property.Name}' identity must be one scalar property of '{element.Name}'.");
                }

                ValidateScope(children.Scope, new(elementProperties, identity.Type, level.Identity, ProjectionLevelKind.Children, level.IdentifierName));
            }

            foreach (var nested in scope.Nested)
            {
                RejectNull(nested.Scope, "projection nested scope");
                if (!structural.Add(nested.Property) || !level.Targets.TryGetValue(nested.Property, out var property) ||
                    property.Type is not { Kind: SemanticTypeReferenceKind.CompositeType, IsCollection: false } ||
                    !_types.TryGetValue(property.Type.Target, out var composite))
                {
                    throw new InvalidSemanticContract($"Projection nested property '{nested.Property}' must be one composite property on its level.");
                }

                // A nested object lives in its enclosing instance, so its keys keep addressing that instance.
                ValidateScope(nested.Scope, level with { Targets = Properties(composite.Properties), Kind = ProjectionLevelKind.Nested });
            }
        }

        void ValidateEvery(SemanticProjectionEvery? every, ProjectionLevel level)
        {
            if (every is null)
            {
                return;
            }

            if (every.SubscribesToAllEvents && (level.Kind != ProjectionLevelKind.Root || !every.IncludeChildren))
            {
                throw new InvalidSemanticContract("Only a projection's own level subscribes to all events, and it always includes children.");
            }

            ValidateProjectionMappings(every.Mappings, level.Targets, null);
        }

        void ValidateParentKey(SemanticProjectionKey? parentKey, ProjectionLevel level, Dictionary<SemanticId, SemanticProperty> sources)
        {
            if (level.ParentIdentity is null)
            {
                if (parentKey is not null)
                {
                    throw new InvalidSemanticContract("A projection parent key is only meaningful inside a child collection.");
                }

                return;
            }

            if (parentKey is null)
            {
                throw new InvalidSemanticContract("A projection block inside a child collection requires a parent key.");
            }

            ValidateProjectionKey(parentKey, level.ParentIdentity, sources, "parent key");
        }

        void RequireUniqueEvent(HashSet<SemanticId> seen, SemanticId eventContract)
        {
            if (!seen.Add(eventContract))
            {
                throw new InvalidSemanticContract($"Projection event contract '{eventContract}' is used more than once on one level.");
            }
        }

        Dictionary<SemanticId, SemanticProperty> EventProperties(SemanticId eventContract) =>
            _events.TryGetValue(eventContract, out var contract)
                ? Properties(contract.Properties)
                : throw new InvalidSemanticContract($"Projection event contract '{eventContract}' is unresolved.");

        sealed record ProjectionLevel(
            Dictionary<SemanticId, SemanticProperty> Targets,
            SemanticTypeReference Identity,
            SemanticTypeReference? ParentIdentity,
            ProjectionLevelKind Kind,
            string IdentifierName);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        // Inherit resolves to the projection's own setting (ProjectionDefinitionSyntaxVisitor.GetAutoMapValue, :317-325).
        static bool AutoMap(AutoMapMode mode, AutoMapMode projection) => mode switch
        {
            AutoMapMode.Enabled => true,
            AutoMapMode.Disabled => false,
            _ => projection != AutoMapMode.Disabled
        };

        // One recursive block processor for the projection body, a children body and a nested body, with no per-level block
        // restrictions - exactly Chronicle's ProjectionDefinitionSyntaxVisitor.ProcessBlocks (ProjectionDefinitionSyntaxVisitor.cs:59-107).
        SemanticProjectionScope BindScope(IEnumerable<ProjectionBlockSyntax> blocks, ProjectionLevel level, AutoMapMode projectionAutoMap)
        {
            var from = ImmutableArray.CreateBuilder<SemanticProjectionFrom>();
            var joins = ImmutableArray.CreateBuilder<SemanticProjectionJoin>();
            var children = ImmutableArray.CreateBuilder<SemanticProjectionChildren>();
            var nested = ImmutableArray.CreateBuilder<SemanticProjectionNested>();
            var removals = ImmutableArray.CreateBuilder<SemanticProjectionRemoval>();
            var joinRemovals = ImmutableArray.CreateBuilder<SemanticProjectionJoinRemoval>();
            SemanticProjectionEvery? every = null;

            // Chronicle ProjectionValidator.ValidateDuplicateEvents (ProjectionValidator.cs:92-138).
            var levelEvents = new HashSet<string>(StringComparer.Ordinal);
            var joinEvents = new HashSet<string>(StringComparer.Ordinal);
            foreach (var block in blocks)
            {
                switch (block)
                {
                    case FromSyntax value:
                        from.AddRange(BindFrom(value, level, levelEvents));
                        break;
                    case EverySyntax value:
                        every = BindEvery(every, value.Mappings, value.IncludeChildren, false, level, value.Location);
                        break;
                    case AllSyntax value when level.IsRoot:
                        every = BindEvery(every, value.Mappings, true, true, level, value.Location);
                        break;
                    case AllSyntax value:
                        Warning(
                            DiagnosticCodes.PartiallyLoweredProjectionSyntax,
                            $"Projection '{level.Projection}' declares 'all' inside a children or nested block, where Chronicle drops the subscription to every event type (ProjectionDefinitionSyntaxVisitor.ProcessChildren) - it behaves as 'every'.",
                            value.Location);
                        every = BindEvery(every, value.Mappings, true, false, level, value.Location);
                        break;
                    case JoinSyntax value:
                        joins.AddRange(BindJoin(value, level, joinEvents));
                        break;
                    case ChildrenSyntax value when BindChildren(value, level, projectionAutoMap) is { } bound:
                        children.Add(bound);
                        break;
                    case NestedSyntax value when BindNested(value, level, projectionAutoMap) is { } bound:
                        nested.Add(bound);
                        break;
                    case RemoveWithSyntax value when LevelEvent(value.Event, value.Location, levelEvents) is { } @event:
                        removals.Add(new(@event.Contract.Id, BindKeyExpression(value.Key, @event, "removal key"), BindParentKey(value.ParentKey, @event, level)));
                        break;
                    case RemoveViaJoinSyntax value when LevelEvent(value.Event, value.Location, levelEvents) is { } @event:
                        joinRemovals.Add(new(@event.Contract.Id, BindKeyExpression(value.Key, @event, "join removal key")));
                        break;

                    // 'clear with' lowers exactly as 'remove with' without keys; the level decides what it removes (ProjectionDefinitionSyntaxVisitor.cs:98-100).
                    case ClearWithSyntax value when LevelEvent(value.Event, value.Location, levelEvents) is { } @event:
                        removals.Add(new(@event.Contract.Id, SemanticProjectionKey.EventSourceIdentity, level.IsChildContext ? SemanticProjectionKey.EventSourceIdentity : null));
                        break;
                    case ProjectionVariantSyntax value:
                        Error(
                            DiagnosticCodes.UnsupportedSemanticSyntax,
                            $"Projection variant '{value.Name}' is not admitted by ESM v1: variants are tracked by #185, and Chronicle's projection lowering does not handle them yet (Cratis/Chronicle#4109).",
                            value.Location);
                        break;
                    case RemoveWithSyntax or RemoveViaJoinSyntax or ClearWithSyntax or ChildrenSyntax or NestedSyntax:
                        break;
                    default:
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection block '{block.GetType().Name}' is not admitted by ESM v1.", block.Location);
                        break;
                }
            }

            return new(from.ToImmutable(), joins.ToImmutable(), children.ToImmutable(), nested.ToImmutable(), every, removals.ToImmutable(), joinRemovals.ToImmutable());
        }

        // 'from A, B' is one transition per event sharing the mappings (ProjectionDefinitionSyntaxVisitor.ProcessFrom, :109-121).
        IEnumerable<SemanticProjectionFrom> BindFrom(FromSyntax from, ProjectionLevel level, HashSet<string> levelEvents)
        {
            foreach (var spec in from.Events)
            {
                if (LevelEvent(spec.Event, spec.Location, levelEvents) is not { } @event)
                {
                    continue;
                }

                var key = spec.Key is not null ? BindKeyExpression(spec.Key, @event, "transition key") : BindKey(from.Key, @event);
                var parentKey = BindParentKey(from.ParentKey, @event, level);
                yield return new(@event.Contract.Id, key, parentKey, BindMappings(from.Mappings, level, @event, level.AutoMap, false));
            }
        }

        IEnumerable<SemanticProjectionJoin> BindJoin(JoinSyntax join, ProjectionLevel level, HashSet<string> joinEvents)
        {
            if (!level.Targets.TryGetValue(join.On, out var on) || on.Type.IsCollection)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Projection '{level.Projection}' joins on '{join.On}', which is not one scalar property of its level.", join.Location);
                yield break;
            }

            foreach (var joinEvent in join.Events)
            {
                if (LevelEvent(joinEvent.Event, joinEvent.Location, joinEvents) is not { } @event)
                {
                    continue;
                }

                // Chronicle's lowering keeps no per-joined-event auto-map (ProjectionDefinitionSyntaxVisitor.ProcessJoin, :147-156):
                // the level's auto-map applies (ProjectionFactory.cs:680-684).
                if (joinEvent.AutoMap != AutoMapMode.Inherit)
                {
                    Warning(
                        DiagnosticCodes.PartiallyLoweredProjectionSyntax,
                        $"Projection '{level.Projection}' sets auto-map on joined event '{joinEvent.Event}', which Chronicle's projection lowering drops - the level's auto-map applies.",
                        joinEvent.Location);
                }

                yield return new(@event.Contract.Id, on.Id, BindMappings(joinEvent.Mappings, level, @event, level.AutoMap, true));
            }
        }

        SemanticProjectionChildren? BindChildren(ChildrenSyntax children, ProjectionLevel level, AutoMapMode projectionAutoMap)
        {
            // Chronicle ProjectionValidator.ValidateChildren (ProjectionValidator.cs:217-238).
            if (!level.Targets.TryGetValue(children.Property, out var property) ||
                property.Type is not { Kind: SemanticTypeReferenceKind.CompositeType, IsCollection: true } ||
                CompositeProperties(property.Type) is not { } element)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Projection '{level.Projection}' children property '{children.Property}' must be a collection of a composite type.", children.Location);
                return null;
            }

            if (children.IdentifiedBy is not PathExpressionSyntax identifiedBy || !element.TryGetValue(identifiedBy.Path, out var identity))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Projection '{level.Projection}' children '{children.Property}' must be identified by one property of its element type.", children.IdentifiedBy.Location);
                return null;
            }

            var childLevel = level with { Targets = element, AutoMap = AutoMap(children.AutoMap, projectionAutoMap), IsRoot = false, IsChildContext = true };
            return new(property.Id, identity.Id, BindScope(children.Blocks, childLevel, projectionAutoMap));
        }

        SemanticProjectionNested? BindNested(NestedSyntax nested, ProjectionLevel level, AutoMapMode projectionAutoMap)
        {
            if (!level.Targets.TryGetValue(nested.Property, out var property) ||
                property.Type is not { Kind: SemanticTypeReferenceKind.CompositeType, IsCollection: false } ||
                CompositeProperties(property.Type) is not { } composite)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Projection '{level.Projection}' nested property '{nested.Property}' must be one composite property.", nested.Location);
                return null;
            }

            // A nested object lives in its enclosing instance, so the level keeps addressing that instance (ProjectionFactory.cs:925-954).
            var nestedLevel = level with { Targets = composite, AutoMap = AutoMap(nested.AutoMap, projectionAutoMap), IsRoot = false };
            return new(property.Id, BindScope(nested.Blocks, nestedLevel, projectionAutoMap));
        }

        SemanticProjectionEvery BindEvery(
            SemanticProjectionEvery? existing,
            IEnumerable<MappingSyntax> mappings,
            bool includeChildren,
            bool subscribesToAllEvents,
            ProjectionLevel level,
            SourceLocation location)
        {
            if (existing is not null)
            {
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Projection '{level.Projection}' declares more than one 'every' or 'all' block on one level; Chronicle keeps only the last at a projection's own level and merges them below it, so ESM v1 admits one.",
                    location);
            }

            return new(includeChildren, subscribesToAllEvents, BindMappings(mappings, level, null, false, false));
        }

        SemanticProjectionKey? BindParentKey(ExpressionSyntax? parentKey, BoundEvent @event, ProjectionLevel level)
        {
            if (!level.IsChildContext)
            {
                if (parentKey is not null)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection '{level.Projection}' declares a parent key outside a children block, where Chronicle never reads it.", parentKey.Location);
                }

                return null;
            }

            // An absent parent key is the event source identity (Chronicle ProjectionFactory.cs:990-997).
            return BindKeyExpression(parentKey, @event, "parent key");
        }

        BoundEvent? LevelEvent(string name, SourceLocation location, HashSet<string> seen)
        {
            if (!_events.TryGetValue(name, out var @event))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Event type '{name}' not found.", location);
                return null;
            }

            if (!seen.Add(name))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Duplicate event type '{name}' - event types can only be used once at each level.", location);
                return null;
            }

            return @event;
        }

        void Warning(string code, string message, SourceLocation location) =>
            _diagnostics.Add(new(DiagnosticSeverity.Warning, code, message, location));
    }
}

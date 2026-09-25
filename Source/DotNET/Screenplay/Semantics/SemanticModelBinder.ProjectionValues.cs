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
        Dictionary<SemanticId, Dictionary<string, SemanticProperty>>? _compositeProperties;

        SemanticProjectionKey BindKey(KeySyntax? key, BoundEvent @event)
        {
            switch (key)
            {
                case null:
                    return SemanticProjectionKey.EventSourceIdentity;
                case ExpressionKeySyntax expression:
                    return BindKeyExpression(expression.Expression, @event, "transition key");
                case CompositeKeySyntax composite:
                    if (!_types.TryGetValue(composite.Type, out var type) ||
                        CompositeProperties(SemanticTypeReference.ForCompositeType(type.Id)) is not { } properties)
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Composite key type '{composite.Type}' not found.", composite.Location);
                        return SemanticProjectionKey.EventSourceIdentity;
                    }

                    var parts = ImmutableArray.CreateBuilder<SemanticProjectionKeyPart>();
                    foreach (var part in composite.Parts)
                    {
                        if (!properties.TryGetValue(part.Property, out var property))
                        {
                            Error(DiagnosticCodes.InvalidSemanticBinding, $"Property '{part.Property}' not found in composite key type '{composite.Type}'.", part.Location);
                            continue;
                        }

                        if (BindKeyValue(part.Expression, @event, "composite key part") is { } value)
                        {
                            parts.Add(new(property.Id, value));
                        }
                    }

                    return new SemanticProjectionCompositeKey(type.Id, parts.ToImmutable());
                default:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection key '{key.GetType().Name}' is not admitted by ESM v1.", key.Location);
                    return SemanticProjectionKey.EventSourceIdentity;
            }
        }

        // With no key, and for 'key $eventSourceId', Chronicle keys on the event source identity (ProjectionFactory.cs:1009-1017,
        // Generator.cs:168-169): both are the default key.
        SemanticProjectionKey BindKeyExpression(ExpressionSyntax? expression, BoundEvent @event, string description) =>
            expression is null || BindKeyValue(expression, @event, description) is not { } value
                ? SemanticProjectionKey.EventSourceIdentity
                : new SemanticProjectionValueKey(value);

        SemanticProjectionValue? BindKeyValue(ExpressionSyntax expression, BoundEvent @event, string description)
        {
            switch (expression)
            {
                // Chronicle stores only a text literal key as a value; any other literal is read back as a property path
                // (ProjectionDefinitionSyntaxVisitor.ConvertKeyExpression, :263-268).
                case LiteralExpressionSyntax { Value: not string }:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"A literal projection {description} must be text (literal \"...\"): Chronicle reads any other literal key as a property path.", expression.Location);
                    return null;

                // Chronicle ProjectionValidator.ValidateCompositeKey (ProjectionValidator.cs:457-459).
                case TemplateExpressionSyntax:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Template expressions are not supported in a projection {description}. Use simple expressions only.", expression.Location);
                    return null;
                default:
                    return BindProjectionValue(expression, @event, description);
            }
        }

        ImmutableArray<SemanticProjectionMapping> BindMappings(
            IEnumerable<MappingSyntax> mappings,
            ProjectionLevel level,
            BoundEvent? @event,
            bool autoMap,
            bool isJoin)
        {
            var bound = ImmutableArray.CreateBuilder<SemanticProjectionMapping>();
            foreach (var mapping in mappings)
            {
                if (BindMapping(mapping, level, @event) is { } value)
                {
                    bound.Add(value);
                }
            }

            if (autoMap && @event is not null)
            {
                bound.AddRange(AutoMapped([.. mappings], level, @event, isJoin));
            }

            return bound.ToImmutable();
        }

        SemanticProjectionMapping? BindMapping(MappingSyntax mapping, ProjectionLevel level, BoundEvent? @event)
        {
            if (mapping.Property.Contains('$', StringComparison.Ordinal))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection mapping '{mapping.Property}' uses a dynamic dictionary key, which ESM v1 does not admit.", mapping.Location);
                return null;
            }

            if (ResolvePath(mapping.Property, level.Targets) is not { } target)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Read model property '{mapping.Property}' not found.", mapping.Location);
                return null;
            }

            // Chronicle lowers 'x = null' to the clear expression and 'count' to exactly what 'increment' does
            // (ProjectionDefinitionSyntaxVisitor.cs:209-245, PropertyMappers.cs:106-153).
            return mapping switch
            {
                SetMappingSyntax { Source: LiteralExpressionSyntax { Value: null } } => new(target.Path, SemanticProjectionOperation.Clear, null),
                SetMappingSyntax set => Source(SemanticProjectionOperation.Set, set.Source),
                ClearMappingSyntax => new(target.Path, SemanticProjectionOperation.Clear, null),
                AddMappingSyntax add => Source(SemanticProjectionOperation.Add, add.Value),
                SubtractMappingSyntax subtract => Source(SemanticProjectionOperation.Subtract, subtract.Value),
                IncrementMappingSyntax or CountMappingSyntax => new(target.Path, SemanticProjectionOperation.Increment, null),
                DecrementMappingSyntax => new(target.Path, SemanticProjectionOperation.Decrement, null),
                _ => Unsupported()
            };

            SemanticProjectionMapping? Source(SemanticProjectionOperation operation, ExpressionSyntax source) =>
                BindProjectionValue(source, @event, "projection mapping") is { } value ? new(target.Path, operation, value) : null;

            SemanticProjectionMapping? Unsupported()
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Projection mapping '{mapping.GetType().Name}' is not admitted by ESM v1.", mapping.Location);
                return null;
            }
        }

        // Chronicle auto-maps by case-insensitive name, skipping explicitly mapped targets (and, for a join, event properties
        // already used as sources), and skips aggregate-only from blocks (ProjectionFactory.cs:178-267). A name match whose types
        // differ is left unmapped, because ESM cannot express Chronicle's runtime conversion.
        IEnumerable<SemanticProjectionMapping> AutoMapped(MappingSyntax[] mappings, ProjectionLevel level, BoundEvent @event, bool isJoin)
        {
            var aggregateOnly = mappings.Length > 0 && mappings.All(_ => _ is AddMappingSyntax or SubtractMappingSyntax or IncrementMappingSyntax or DecrementMappingSyntax or CountMappingSyntax);
            if (!isJoin && aggregateOnly)
            {
                yield break;
            }

            var mapped = mappings.Select(_ => _.Property.Split('.')[^1]).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var sources = isJoin ? JoinAutoMapSources.ExplicitlyMapped(mappings) : [];
            foreach (var source in @event.Contract.Properties)
            {
                var target = level.Targets.Values.FirstOrDefault(_ => string.Equals(_.Name, source.Name, StringComparison.OrdinalIgnoreCase));
                if (target is null || mapped.Contains(target.Name) || sources.Contains(source.Name) ||
                    target.Type.Kind != source.Type.Kind || target.Type.Primitive != source.Type.Primitive || target.Type.Target != source.Type.Target ||
                    target.Type.IsCollection != source.Type.IsCollection || (source.Type.IsOptional && !target.Type.IsOptional))
                {
                    continue;
                }

                mapped.Add(target.Name);
                yield return new([target.Id], SemanticProjectionOperation.Set, SemanticProjectionValue.EventProperty([source.Id]));
            }
        }

        SemanticProjectionValue? BindProjectionValue(ExpressionSyntax expression, BoundEvent? @event, string description)
        {
            switch (expression)
            {
                case PathExpressionSyntax path when @event is null:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"An 'every' or 'all' mapping cannot read event property '{path.Path}': the block has no single event contract.", expression.Location);
                    return null;
                case PathExpressionSyntax path:
                    if (ResolvePath(path.Path, @event.Properties) is { } source)
                    {
                        return SemanticProjectionValue.EventProperty(source.Path);
                    }

                    var historical = @event.Contract.PriorRevisions.LastOrDefault(revision =>
                        revision.Properties.Any(property => property.Name == path.Path.Split('.')[0]));
                    if (historical is not null)
                    {
                        Error(
                            DiagnosticCodes.InvalidSemanticBinding,
                            $"Event '{@event.Contract.Name}' revision {historical.Revision.Value} declares property '{path.Path}', but current revision {@event.Contract.Revision.Value} does not; historical-shape references are unsupported.",
                            expression.Location);
                        return null;
                    }

                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Event property '{path.Path}' not found on '{@event.Contract.Name}'.", expression.Location);
                    return null;
                case LiteralExpressionSyntax literal:
                    return SemanticProjectionValue.Literal(BindLiteral(literal));
                case EventSourceIdExpressionSyntax:
                    return SemanticProjectionValue.EventSourceIdentity;
                case EventContextExpressionSyntax context:
                    var scalar = SemanticEventContextScalars.Resolve(context.Path);
                    switch (scalar.Kind)
                    {
                        case SemanticEventContextScalarKind.EventSource:
                            return SemanticProjectionValue.EventSourceIdentity;
                        case SemanticEventContextScalarKind.Scalar:
                            return SemanticProjectionValue.EventContext(scalar.Path);
                    }

                    Error(
                        DiagnosticCodes.InvalidSemanticBinding,
                        $"'$eventContext.{context.Path}' is not a path ESM v1 admits on Chronicle's event context: {scalar.Reason}. Admitted paths are {string.Join(", ", SemanticEventContextScalars.All)}.",
                        expression.Location);
                    return null;

                // Chronicle compiles '$causedBy' but has no runtime resolver for it (Cratis/Chronicle#4119).
                case CausedByExpressionSyntax causedBy:
                    Error(
                        DiagnosticCodes.UnsupportedSemanticSyntax,
                        $"'$causedBy' has no runtime resolver in Chronicle (Cratis/Chronicle#4119); use '$eventContext.causedBy.{causedBy.Property ?? "subject"}' instead.",
                        expression.Location);
                    return null;
                default:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"The {description} expression '{expression.GetType().Name}' is not admitted by ESM v1.", expression.Location);
                    return null;
            }
        }

        (ImmutableArray<SemanticId> Path, SemanticProperty Property)? ResolvePath(string path, Dictionary<string, SemanticProperty> root)
        {
            var ids = ImmutableArray.CreateBuilder<SemanticId>();
            var properties = root;
            SemanticProperty? property = null;
            foreach (var segment in path.Split('.'))
            {
                if (property is not null)
                {
                    if (property.Type.IsCollection || CompositeProperties(property.Type) is not { } next)
                    {
                        return null;
                    }

                    properties = next;
                }

                if (!properties.TryGetValue(segment, out property))
                {
                    return null;
                }

                ids.Add(property.Id);
            }

            return property is null ? null : (ids.ToImmutable(), property);
        }

        // Composite type properties resolve through the identity catalog without mapping a source location a second time.
        Dictionary<string, SemanticProperty>? CompositeProperties(SemanticTypeReference type)
        {
            if (type.Kind != SemanticTypeReferenceKind.CompositeType)
            {
                return null;
            }

            if (_compositeProperties is null)
            {
                _compositeProperties = [];
                foreach (var declaration in syntax.Types ?? [])
                {
                    var (address, id) = _types[declaration.Name];
                    _compositeProperties[id] = declaration.Properties
                        .Select(property => new SemanticProperty(
                            documents.IdentityCatalog.ResolveSemantic(SemanticAddress.ForProperty(address, property.Name)),
                            property.Name,
                            BindTypeReference(property.Type),
                            false))
                        .ToDictionary(_ => _.Name, StringComparer.Ordinal);
                }
            }

            return _compositeProperties.GetValueOrDefault(type.Target);
        }
    }
}

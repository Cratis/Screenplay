// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Validates projection keys, values and mappings in the scoped projection shape.
/// </summary>
internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        void ValidateProjectionKey(
            SemanticProjectionKey key,
            SemanticTypeReference identity,
            Dictionary<SemanticId, SemanticProperty> sources,
            string description)
        {
            RejectNull(key, description);
            ValidateEnum(key.Kind, SemanticProjectionKeyKind.Unknown, "projection key kind");
            switch (key)
            {
                case SemanticProjectionValueKey value when key.Kind == SemanticProjectionKeyKind.Value:
                    ValidateKeyValue(value.Value, identity, sources, description);
                    return;
                case SemanticProjectionCompositeKey composite when key.Kind == SemanticProjectionKeyKind.Composite:
                    ValidateCompositeKey(composite, identity, sources);
                    return;
                default:
                    throw new InvalidSemanticContract("A projection key variant is malformed or unknown.");
            }
        }

        // Chronicle's ProjectionValidator.ValidateCompositeKey (ProjectionValidator.cs:383-465): the key type must exist and be a
        // complex type, and every part must name one of its properties with a simple value expression.
        void ValidateCompositeKey(
            SemanticProjectionCompositeKey key,
            SemanticTypeReference identity,
            Dictionary<SemanticId, SemanticProperty> sources)
        {
            if (!_types.TryGetValue(key.Type, out var type))
            {
                throw new InvalidSemanticContract($"Composite key type '{key.Type}' is unresolved.");
            }

            if (identity is not { Kind: SemanticTypeReferenceKind.CompositeType, IsCollection: false } || identity.Target != key.Type)
            {
                throw new InvalidSemanticContract($"Composite key type '{type.Name}' is not the type of the identity it keys.");
            }

            RequireObjects(key.Parts, nameof(key.Parts), "composite key part");
            var properties = Properties(type.Properties);
            var assigned = new HashSet<SemanticId>();
            foreach (var part in key.Parts)
            {
                if (!assigned.Add(part.Property) || !properties.TryGetValue(part.Property, out var property))
                {
                    throw new InvalidSemanticContract($"Composite key part '{part.Property}' is duplicated or not a property of '{type.Name}'.");
                }

                ValidateKeyValue(part.Value, property.Type, sources, "composite key part");
            }
        }

        void ValidateKeyValue(
            SemanticProjectionValue value,
            SemanticTypeReference identity,
            Dictionary<SemanticId, SemanticProperty> sources,
            string description)
        {
            var type = ResolveProjectionValue(value, sources, out var untyped);

            // Chronicle stores only a text literal key as a value; any other literal is read back as a property path
            // (ProjectionDefinitionSyntaxVisitor.cs:263-268).
            if (value is SemanticProjectionLiteral { Value: not SemanticTextValue })
            {
                throw new InvalidSemanticContract($"A literal {description} must be text.");
            }

            if (value is SemanticProjectionLiteral literal)
            {
                ValidateValue(literal.Value, identity with { IsOptional = false }, description);
                return;
            }

            if (value is SemanticProjectionEventContextValue)
            {
                if (identity.IsCollection || UnderlyingPrimitive(identity) != type!.Primitive)
                {
                    throw new InvalidSemanticContract($"A {description} event-context type is incompatible with the identity it keys.");
                }

                return;
            }

            if (!untyped && (type?.IsCollection != false || type.IsOptional || identity.IsCollection || !SameValueType(type, identity)))
            {
                throw new InvalidSemanticContract($"A {description} type is incompatible with the identity it keys.");
            }
        }

        void ValidateProjectionMappings(
            ImmutableArray<SemanticProjectionMapping> mappings,
            Dictionary<SemanticId, SemanticProperty> targets,
            Dictionary<SemanticId, SemanticProperty>? sources)
        {
            RequireObjects(mappings, nameof(mappings), "projection mapping");
            var mapped = new HashSet<string>(StringComparer.Ordinal);
            foreach (var mapping in mappings)
            {
                // Absent intermediates of a target are created (Chronicle ExpandoObjectExtensions.EnsurePath), so only the last
                // segment's own optionality counts.
                var target = ResolvePath(mapping.Target, targets, "projection mapping target", false);
                if (!mapped.Add(string.Join('/', mapping.Target)))
                {
                    throw new InvalidSemanticContract("A projection mapping target is duplicated.");
                }

                ValidateEnum(mapping.Operation, SemanticProjectionOperation.Unknown, "projection operation");
                var takesSource = mapping.Operation is SemanticProjectionOperation.Set or SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract;
                if (takesSource != (mapping.Source is not null))
                {
                    throw new InvalidSemanticContract($"Projection operation '{mapping.Operation}' has an invalid source shape.");
                }

                switch (mapping.Operation)
                {
                    case SemanticProjectionOperation.Set:
                        ValidateSet(mapping.Source!, target.Type, sources);
                        break;
                    case SemanticProjectionOperation.Clear when !target.Type.IsOptional:
                        throw new InvalidSemanticContract("A projection clear requires an optional target.");
                    case SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract
                        or SemanticProjectionOperation.Increment or SemanticProjectionOperation.Decrement:
                        RequireNumeric(target.Type, "projection arithmetic target");
                        if (mapping.Source is not null)
                        {
                            RequireNumeric(ResolveProjectionValue(mapping.Source, sources, out _), "projection arithmetic operand");
                        }

                        break;
                }
            }
        }

        void ValidateSet(SemanticProjectionValue source, SemanticTypeReference target, Dictionary<SemanticId, SemanticProperty>? sources)
        {
            var type = ResolveProjectionValue(source, sources, out var untyped);
            switch (source)
            {
                // Chronicle lowers 'x = null' to the clear expression (ProjectionDefinitionSyntaxVisitor.cs:242-245), so a null
                // literal set has exactly one spelling: Clear.
                case SemanticProjectionLiteral { Value: SemanticNullValue }:
                    throw new InvalidSemanticContract("A null projection set is a clear.");
                case SemanticProjectionLiteral literal:
                    ValidateValue(literal.Value, target, "projection mapping");
                    return;
                case SemanticProjectionEventContextValue when target.IsCollection || UnderlyingPrimitive(target) != type!.Primitive:
                    throw new InvalidSemanticContract("A projection event-context source and target have incompatible types.");
                case SemanticProjectionEventContextValue:
                    return;
            }

            if (untyped)
            {
                if (target.IsCollection)
                {
                    throw new InvalidSemanticContract("An event source identity cannot set a collection property.");
                }

                return;
            }

            RequireCompatible(type, target, "projection mapping");
        }

        SemanticTypeReference? ResolveProjectionValue(
            SemanticProjectionValue value,
            Dictionary<SemanticId, SemanticProperty>? sources,
            out bool untyped)
        {
            RejectNull(value, "projection value");
            ValidateEnum(value.Kind, SemanticProjectionValueKind.Unknown, "projection value kind");
            untyped = false;
            switch (value)
            {
                case SemanticProjectionLiteral literal when value.Kind == SemanticProjectionValueKind.Literal:
                    RejectNull(literal.Value, "semantic value");
                    ValidateValueVariant(literal.Value);
                    return literal.Value is SemanticArrayValue or SemanticCompositeValue ? null : TypeOf(literal.Value);
                case SemanticProjectionEventProperty property when value.Kind == SemanticProjectionValueKind.EventProperty:
                    return sources is null
                        ? throw new InvalidSemanticContract("An every mapping cannot read an event property, because it has no single event contract.")
                        : ResolvePath(property.Path, sources, "projection event property", true).Type;
                case SemanticProjectionEventSourceIdentity when value.Kind == SemanticProjectionValueKind.EventSourceIdentity:
                    untyped = true;
                    return null;
                case SemanticProjectionEventContextValue context when value.Kind == SemanticProjectionValueKind.EventContext:
                    return SemanticEventContextScalars.Resolve(context.Path ?? string.Empty) is { Kind: SemanticEventContextScalarKind.Scalar } scalar &&
                        scalar.Path == context.Path
                        ? SemanticTypeReference.ForPrimitive(scalar.Primitive)
                        : throw new InvalidSemanticContract($"Event-context path '{context.Path}' is not an admitted canonical scalar path on Chronicle's event context.");
                default:
                    throw new InvalidSemanticContract("A projection value variant is malformed or unknown.");
            }
        }

        SemanticProperty ResolvePath(
            ImmutableArray<SemanticId> path,
            Dictionary<SemanticId, SemanticProperty> root,
            string description,
            bool optionalThroughIntermediates)
        {
            RequireArray(path, nameof(path));
            if (path.IsEmpty)
            {
                throw new InvalidSemanticContract($"A {description} path cannot be empty.");
            }

            var properties = root;
            SemanticProperty? property = null;
            var optional = false;
            foreach (var segment in path)
            {
                if (property is not null)
                {
                    // An intermediate segment navigates into one composite object (Chronicle ProjectionValidator.cs:349-381).
                    if (property.Type is not { Kind: SemanticTypeReferenceKind.CompositeType, IsCollection: false } || !_types.TryGetValue(property.Type.Target, out var composite))
                    {
                        throw new InvalidSemanticContract($"A {description} path navigates through '{property.Name}', which is not one composite object.");
                    }

                    optional |= property.Type.IsOptional;
                    properties = Properties(composite.Properties);
                }

                if (!properties.TryGetValue(segment, out property))
                {
                    throw new InvalidSemanticContract($"A {description} path segment '{segment}' is unresolved.");
                }
            }

            return optional && optionalThroughIntermediates ? property! with { Type = property!.Type with { IsOptional = true } } : property!;
        }

        void RequireNumeric(SemanticTypeReference? type, string description)
        {
            if (type?.IsCollection != false || UnderlyingPrimitive(type) is not (SemanticPrimitiveType.WholeNumber or SemanticPrimitiveType.DecimalNumber))
            {
                throw new InvalidSemanticContract($"A {description} must be one numeric value.");
            }
        }
    }
}

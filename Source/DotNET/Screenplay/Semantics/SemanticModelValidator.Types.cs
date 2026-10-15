// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        void ValidateProperties(ImmutableArray<SemanticProperty> properties, bool command = false)
        {
            foreach (var property in properties)
            {
                ValidateTypeReference(property.Type);
                if (property.IsGenerated && !IsAdmittedGeneratedProperty(property, command))
                {
                    throw new InvalidSemanticContract("A generated property requires an ESM v7 command, a generatable scalar concept (rules require ESM v20), and at least 64 random bits when it is an identifier.");
                }
            }
        }

        bool IsAdmittedGeneratedProperty(SemanticProperty property, bool command) =>
            command && _semanticVersion.IsAtLeast(SemanticVersion.V7) &&
            property.Type is { Kind: SemanticTypeReferenceKind.Concept, IsCollection: false, IsOptional: false } type &&
            _concepts.TryGetValue(type.Target, out var concept) &&
            (concept.Primitive == SemanticPrimitiveType.Uuid || concept.Generation is not null) &&
            (concept.Compliance is null || concept.Generation is null) && (concept.Validations.IsEmpty || _semanticVersion.IsAtLeast(SemanticVersion.V20)) &&
            (!property.IsIdentifier || (concept.Primitive is SemanticPrimitiveType.Uuid or SemanticPrimitiveType.Text && SemanticGenerationContract.HasIdentifierSupport(concept.Generation, _utcAssessmentYear)));

        void ValidateTypeReference(SemanticTypeReference type)
        {
            RejectNull(type, "type reference");
            ValidateEnum(type.Kind, SemanticTypeReferenceKind.Unknown, "type reference kind");
            if (!IsResolvedTypeReference(type))
            {
                throw new InvalidSemanticContract("A semantic type reference is malformed or unresolved.");
            }
        }

        bool IsResolvedTypeReference(SemanticTypeReference type) => type.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => Enum.IsDefined(type.Primitive) && type.Primitive != SemanticPrimitiveType.Unknown && !type.Target.IsSet,
            SemanticTypeReferenceKind.Concept => IsResolvedTarget(type, _concepts),
            SemanticTypeReferenceKind.CompositeType => IsResolvedTarget(type, _types),
            _ => false
        };

        bool IsResolvedTarget<T>(SemanticTypeReference type, Dictionary<SemanticId, T> targets) =>
            type.Primitive == SemanticPrimitiveType.Unknown && type.Target.IsSet && targets.ContainsKey(type.Target);

        bool IsEnumeration(SemanticTypeReference type) =>
            type.Kind == SemanticTypeReferenceKind.Concept &&
            _concepts.TryGetValue(type.Target, out var concept) &&
            !concept.Values.IsEmpty;

        void RequireCompatible(SemanticTypeReference? source, SemanticTypeReference target, string description)
        {
            if (source is null)
            {
                if (!target.IsOptional)
                {
                    throw new InvalidSemanticContract($"A null {description} source is incompatible with a required target.");
                }

                return;
            }

            if (!IsCompatibleSource(source, target))
            {
                throw new InvalidSemanticContract($"A {description} source and target have incompatible types.");
            }
        }

        bool IsCompatibleSource(SemanticTypeReference source, SemanticTypeReference target) =>
            SameType(source, target) || (SameValueType(source, target) && !source.IsOptional && target.IsOptional);

        bool SameType(SemanticTypeReference left, SemanticTypeReference right) =>
            SameValueType(left, right) && left.IsCollection == right.IsCollection && left.IsOptional == right.IsOptional;

        bool SameValueType(SemanticTypeReference left, SemanticTypeReference right) =>
            left.Kind == right.Kind && left.Primitive == right.Primitive && left.Target == right.Target;

        SemanticPrimitiveType UnderlyingPrimitive(SemanticTypeReference type) => type.Kind switch
        {
            SemanticTypeReferenceKind.Primitive => type.Primitive,
            SemanticTypeReferenceKind.Concept when _concepts.TryGetValue(type.Target, out var concept) => concept.Primitive,
            _ => SemanticPrimitiveType.Unknown
        };

        void RequireBoolean(SemanticTypeReference? type, string description)
        {
            if (!IsRequiredScalar(type) || UnderlyingPrimitive(type!) != SemanticPrimitiveType.Boolean)
            {
                throw new InvalidSemanticContract($"A {description} must resolve to required scalar Boolean.");
            }
        }

        bool IsRequiredScalar(SemanticTypeReference? type) => type?.IsCollection is false && !type.IsOptional;

        SemanticProperty IdentifierProperty(SemanticReadModel readModel)
        {
            var identifiers = readModel.Properties.Where(_ => _.IsIdentifier).ToArray();
            if (identifiers.Length != 1 || identifiers[0].Type.IsCollection || identifiers[0].Type.IsOptional)
            {
                throw new InvalidSemanticContract($"Read model '{readModel.Name}' must have exactly one required scalar identifier property.");
            }

            return identifiers[0];
        }

        void ValidatePrimitive(SemanticPrimitiveType primitive) =>
            ValidateEnum(primitive, SemanticPrimitiveType.Unknown, "primitive type");
    }
}

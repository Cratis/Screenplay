// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        SemanticProperty[] ReadModelKeyProperties(SemanticReadModel readModel)
        {
            var keys = readModel.Properties.Where(property => property.IsIdentifier).ToArray();
            if (keys.Length == 0 || keys.Any(property => property.Type.IsCollection || property.Type.IsOptional) ||
                (keys.Length > 1 && (!_semanticVersion.IsAtLeast(SemanticVersion.V27) || keys.Any(property => property.Type.Kind == SemanticTypeReferenceKind.CompositeType))))
            {
                throw new InvalidSemanticContract($"Read model '{readModel.Name}' must have exactly one required scalar identifier property.");
            }

            return keys;
        }

        void ValidateReadModelKeyValue(SemanticReadModel readModel, SemanticValue key, string description)
        {
            var keys = ReadModelKeyProperties(readModel);
            if (keys.Length == 1)
            {
                ValidateValue(key, keys[0].Type, description);
                return;
            }
            if (key is not SemanticCompositeValue composite)
            {
                throw new InvalidSemanticContract($"The {description} requires every named key part.");
            }
            ValidatePropertyValues(composite.Properties, [.. keys], true);
        }

        void ValidateQueryKeyParts(SemanticKeyedQuery query, SemanticReadModel readModel)
        {
            var keys = ReadModelKeyProperties(readModel);
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V27) || keys.Length < 2 || query.Argument is not null || query.KeyProperty is not null ||
                query.Cardinality != SemanticQueryCardinality.ZeroOrOne || !query.Filters.IsEmpty || query.KeyParts.Length != keys.Length)
            {
                throw new InvalidSemanticContract("A query by parts requires a complete caller-supplied key and an optional single composite-key read model.");
            }
            var assigned = new HashSet<SemanticId>();
            foreach (var part in query.KeyParts)
            {
                var property = keys.SingleOrDefault(property => property.Id == part.KeyProperty);
                ValidateTypeReference(part.Type);
                if (property is null || !assigned.Add(part.KeyProperty) || part.Name != property.Name || !IsRequiredScalar(part.Type) || !SameType(part.Type, property.Type))
                {
                    throw new InvalidSemanticContract("A query key part must name one distinct required key property with its declared type.");
                }
            }
        }
    }
}

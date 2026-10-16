// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

public static partial class SemanticValidationRules
{
    /// <summary>
    /// Checks concept rules wherever they occur in a value, including collection elements and composite members.
    /// </summary>
    /// <param name="application">The contract-validated application containing the referenced concepts and types.</param>
    /// <param name="type">The declared type of the value.</param>
    /// <param name="value">The contract-validated value.</param>
    /// <returns>Results in value traversal order and, within each concept, rule declaration order. Primitive values carry no rules.</returns>
    /// <exception cref="InvalidSemanticContract">A value or operand is incompatible with a rule.</exception>
    public static ImmutableArray<SemanticRuleCheck> Check(SemanticApplication application, SemanticTypeReference type, SemanticValue value)
    {
        var results = ImmutableArray.CreateBuilder<SemanticRuleCheck>();
        Check(application.Concepts.ToDictionary(concept => concept.Id), application.Types.ToDictionary(composite => composite.Id), type, value, results);

        return results.ToImmutable();
    }

    static void Check(
        Dictionary<SemanticId, SemanticConcept> concepts,
        Dictionary<SemanticId, SemanticCompositeType> types,
        SemanticTypeReference type,
        SemanticValue value,
        ImmutableArray<SemanticRuleCheck>.Builder results)
    {
        if (type.IsCollection)
        {
            if (value is SemanticArrayValue array)
            {
                var elementType = type with { IsCollection = false, IsOptional = false };
                foreach (var element in array.Values)
                {
                    Check(concepts, types, elementType, element, results);
                }
            }

            return;
        }

        switch (type.Kind)
        {
            case SemanticTypeReferenceKind.Concept:
                results.AddRange(Check(concepts[type.Target], value));
                break;
            case SemanticTypeReferenceKind.CompositeType when value is SemanticCompositeValue composite:
                var properties = types[type.Target].Properties.ToDictionary(property => property.Id);
                foreach (var property in composite.Properties)
                {
                    Check(concepts, types, properties[property.TargetProperty].Type, property.Value, results);
                }

                break;
        }
    }
}

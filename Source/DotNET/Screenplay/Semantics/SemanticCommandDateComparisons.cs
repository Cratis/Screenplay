// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static class SemanticCommandDateComparisons
{
    internal static SemanticPrimitiveType Primitive(SemanticApplication application, SemanticTypeReference type) => type.Kind switch
    {
        SemanticTypeReferenceKind.Primitive => type.Primitive,
        SemanticTypeReferenceKind.Concept => application.Concepts.FirstOrDefault(concept => concept.Id == type.Target)?.Primitive ?? SemanticPrimitiveType.Unknown,
        _ => SemanticPrimitiveType.Unknown
    };

    internal static IEnumerable<SemanticProperty> Properties(SemanticApplication application, SemanticCommand command)
    {
        var conditions = command.Requirements.Select(requirement => requirement.Condition)
            .Concat(command.Produces.Select(production => production.When).OfType<SemanticCondition>());
        var ids = conditions.SelectMany(Operands).Where(operand => operand.Read is null && operand.Property.IsSet).Select(operand => operand.Property)
            .Concat(command.Validations.Where(rule => SemanticDateComparison.IsComparison(rule.Kind)).Select(rule => rule.Property)).ToHashSet();

        return command.Properties.Where(property => ids.Contains(property.Id) && SemanticDateComparison.IsDate(Primitive(application, property.Type)));
    }

    static IEnumerable<SemanticConditionOperand> Operands(SemanticCondition condition) => condition switch
    {
        SemanticComparison comparison => [comparison.Left, comparison.Right],
        SemanticLogicalCondition logical => Operands(logical.Left).Concat(Operands(logical.Right)),
        _ => []
    };
}

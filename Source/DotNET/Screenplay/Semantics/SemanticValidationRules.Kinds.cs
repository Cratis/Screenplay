// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

public static partial class SemanticValidationRules
{
    static void ValidateKinds(SemanticValidationRule rule, SemanticValue value)
    {
        var compatible = rule.Kind switch
        {
            SemanticValidationRuleKind.Maximum or SemanticValidationRuleKind.Minimum =>
                rule.Operand is SemanticNumberValue && value is SemanticNullValue or SemanticTextValue or SemanticNumberValue,
            SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual =>
                rule.Operand is SemanticNullValue or SemanticTextValue or SemanticNumberValue or SemanticBooleanValue &&
                (value is SemanticNullValue || (value is SemanticTextValue or SemanticNumberValue or SemanticBooleanValue &&
                    (rule.Operand is SemanticNullValue || value.GetType() == rule.Operand.GetType()))),
            SemanticValidationRuleKind.GreaterThan or SemanticValidationRuleKind.GreaterThanOrEqual or
                SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual =>
                rule.Operand is SemanticNumberValue && value is SemanticNullValue or SemanticNumberValue,
            SemanticValidationRuleKind.Length =>
                rule.Operand is SemanticNumberValue && value is SemanticNullValue or SemanticTextValue,
            SemanticValidationRuleKind.Matches or SemanticValidationRuleKind.Checksum or
                SemanticValidationRuleKind.Contains or SemanticValidationRuleKind.NotContains or
                SemanticValidationRuleKind.StartsWith or SemanticValidationRuleKind.EndsWith =>
                rule.Operand is SemanticTextValue && value is SemanticNullValue or SemanticTextValue,
            SemanticValidationRuleKind.AllGreaterThan or SemanticValidationRuleKind.AllGreaterThanOrEqual =>
                rule.Operand is SemanticNumberValue &&
                (value is SemanticNullValue || (value is SemanticArrayValue array && array.Values.All(element => element is SemanticNumberValue))),
            _ => true
        };
        if (!compatible)
        {
            throw SemanticValueRules.Malformed();
        }
    }
}

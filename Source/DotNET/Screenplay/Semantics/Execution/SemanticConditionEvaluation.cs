// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal static class SemanticConditionEvaluation
{
    internal static bool Evaluate(SemanticCondition condition, IReadOnlyDictionary<SemanticId, SemanticValue> values) => condition switch
    {
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Evaluate(logical.Left, values) && Evaluate(logical.Right, values),
        SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Evaluate(logical.Left, values) || Evaluate(logical.Right, values),
        SemanticComparison comparison => Compare(comparison, values),
        _ => throw new InvalidSemanticContract("Unknown condition node or operator.")
    };

    static bool Compare(SemanticComparison comparison, IReadOnlyDictionary<SemanticId, SemanticValue> values)
    {
        var left = Operand(comparison.Left, values);
        var right = Operand(comparison.Right, values);
        if (comparison.Operator == SemanticComparisonOperator.Equal) return SemanticValueRules.AreEqual(left, right);
        if (comparison.Operator == SemanticComparisonOperator.NotEqual) return !SemanticValueRules.AreEqual(left, right);
        if (left is not SemanticNumberValue leftNumber || right is not SemanticNumberValue rightNumber)
            throw new InvalidSemanticContract("Ordering condition requires numeric operands.");
        return comparison.Operator switch
        {
            SemanticComparisonOperator.GreaterThan => leftNumber.Value > rightNumber.Value,
            SemanticComparisonOperator.GreaterThanOrEqual => leftNumber.Value >= rightNumber.Value,
            SemanticComparisonOperator.LessThan => leftNumber.Value < rightNumber.Value,
            SemanticComparisonOperator.LessThanOrEqual => leftNumber.Value <= rightNumber.Value,
            _ => throw new InvalidSemanticContract("Unknown comparison operator.")
        };
    }

    static SemanticValue Operand(SemanticConditionOperand operand, IReadOnlyDictionary<SemanticId, SemanticValue> values) =>
        operand.Property.IsSet ? values[operand.Property] : operand.Value!;
}

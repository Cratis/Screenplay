// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal static class SemanticConditionEvaluation
{
    internal static bool Evaluate(
        SemanticCondition condition,
        IReadOnlyDictionary<SemanticId, SemanticValue> values,
        IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads = null,
        IReadOnlyDictionary<SemanticId, SemanticPrimitiveType>? dateTypes = null) => condition switch
        {
            SemanticLogicalCondition { Operator: SemanticLogicalOperator.And } logical => Evaluate(logical.Left, values, reads, dateTypes) && Evaluate(logical.Right, values, reads, dateTypes),
            SemanticLogicalCondition { Operator: SemanticLogicalOperator.Or } logical => Evaluate(logical.Left, values, reads, dateTypes) || Evaluate(logical.Right, values, reads, dateTypes),
            SemanticComparison comparison => Compare(comparison, values, reads, dateTypes),
            _ => throw new InvalidSemanticContract("Unknown condition node or operator.")
        };

    static bool Compare(
        SemanticComparison comparison,
        IReadOnlyDictionary<SemanticId, SemanticValue> values,
        IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads,
        IReadOnlyDictionary<SemanticId, SemanticPrimitiveType>? dateTypes)
    {
        var left = Operand(comparison.Left, values, reads);
        var right = Operand(comparison.Right, values, reads);
        if (dateTypes is not null && dateTypes.TryGetValue(comparison.Left.Property, out var primitive))
            return CompareDate(comparison.Operator, SemanticDateComparison.Compare(left, right, primitive));
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

    static bool CompareDate(SemanticComparisonOperator op, int result) => op switch
    {
        SemanticComparisonOperator.Equal => result == 0,
        SemanticComparisonOperator.NotEqual => result != 0,
        SemanticComparisonOperator.GreaterThan => result > 0,
        SemanticComparisonOperator.GreaterThanOrEqual => result >= 0,
        SemanticComparisonOperator.LessThan => result < 0,
        SemanticComparisonOperator.LessThanOrEqual => result <= 0,
        _ => throw new InvalidSemanticContract("Unknown date comparison operator.")
    };

    static SemanticValue Operand(
        SemanticConditionOperand operand,
        IReadOnlyDictionary<SemanticId, SemanticValue> values,
        IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? reads) =>
        operand switch
        {
            { Read: { } read } => reads![read].GetValueOrDefault(operand.Property, SemanticValue.Null),
            { Property.IsSet: true } => values[operand.Property],
            _ => operand.Value!
        };
}

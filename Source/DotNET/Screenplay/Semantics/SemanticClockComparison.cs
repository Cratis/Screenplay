// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics;

// Evaluates a condition comparison that has a clock operand or reduces an instant with 'date of'.
internal static class SemanticClockComparison
{
    internal static bool Uses(SemanticComparison comparison) => Uses(comparison.Left) || Uses(comparison.Right);

    // The primitive of a clock comparison is the one both sides share: a clock operand states it, and 'date of' makes it a date.
    internal static bool Compare(SemanticComparison comparison, SemanticConditionInputs inputs)
    {
        var clock = inputs.Clock ?? throw new InvalidSemanticContract("A clock operand needs the instant and calendar zone of the command.");
        var primitive = Primitive(comparison, clock);
        return SemanticConditionEvaluator.CompareDate(comparison.Operator, SemanticDateComparisons.Compare(Operand(comparison.Left, inputs.Values, clock), Operand(comparison.Right, inputs.Values, clock), primitive));
    }

    static SemanticPrimitiveType Primitive(SemanticComparison comparison, SemanticClockContext clock)
    {
        var stated = comparison.Left.Clock ?? comparison.Right.Clock;
        return stated is null ? SemanticPrimitiveType.Date : clock.Primitive(stated);
    }

    static bool Uses(SemanticConditionOperand operand) => operand.Clock is not null || operand.DateOf;

    static SemanticValue Operand(SemanticConditionOperand operand, IReadOnlyDictionary<SemanticId, SemanticValue> values, SemanticClockContext clock)
    {
        if (operand.Clock is { } clockOperand) return clock.Value(clockOperand);
        return operand.DateOf ? clock.DateOf(values[operand.Property]) : values[operand.Property];
    }
}

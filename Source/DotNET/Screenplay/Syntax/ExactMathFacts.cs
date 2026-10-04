// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Provides mathematical facts for explicitly exact numbers, without changing Legacy numeric behavior.
/// </summary>
internal static class ExactMathFacts
{
    internal static bool IsIntegral(ExactNumber number) => decimal.Truncate(number.Value) == number.Value;

    internal static int Compare(ExactNumber left, ExactNumber right) => decimal.Compare(left.Value, right.Value);

    // CLR integer/Decimal boxing is not mathematical identity. Floating-point values are deliberately absent:
    // opting into exact authoring requires an explicit ExactNumber, not recovery of an already rounded number.
    internal static bool Equal(object? left, object? right) =>
        TryValue(left, out var leftValue) && TryValue(right, out var rightValue) && leftValue == rightValue;

    static bool TryValue(object? value, out decimal number)
    {
        switch (value)
        {
            case ExactNumber exact:
                number = exact.Value;
                return true;
            case int integer:
                number = integer;
                return true;
            case long integer:
                number = integer;
                return true;
            case decimal amount:
                number = amount;
                return true;
            default:
                number = 0;
                return false;
        }
    }
}

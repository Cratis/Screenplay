// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics;

// Decision 0087: only admitted command comparisons use chronological equality. Other text equality stays ordinal.
internal static class SemanticDateComparison
{
    internal static bool IsDate(SemanticPrimitiveType primitive) => primitive is SemanticPrimitiveType.Date or SemanticPrimitiveType.DateTime;

    internal static bool IsComparison(SemanticValidationRuleKind kind) => kind is
        SemanticValidationRuleKind.Equal or SemanticValidationRuleKind.NotEqual or
        SemanticValidationRuleKind.GreaterThan or SemanticValidationRuleKind.GreaterThanOrEqual or
        SemanticValidationRuleKind.LessThan or SemanticValidationRuleKind.LessThanOrEqual;

    internal static bool TryInstant(string value, out DateTimeOffset instant) => DateTimeOffset.TryParseExact(
        value,
        ["yyyy-MM-dd'T'HH:mm:ss.FFFFFFFzzz", "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF'Z'", "yyyy-MM-dd'T'HH:mmzzz", "yyyy-MM-dd'T'HH:mm'Z'"],
        CultureInfo.InvariantCulture,
        DateTimeStyles.AssumeUniversal,
        out instant);

    internal static string? Canonical(string value, SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Date when DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        SemanticPrimitiveType.DateTime when TryInstant(value, out var instant) => instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        _ => null
    };

    internal static void ValidateConstant(SemanticValue? value, SemanticPrimitiveType primitive)
    {
        if (value is not SemanticTextValue text || Canonical(text.Value, primitive) != text.Value)
            throw new InvalidSemanticContract("A date comparison constant must use its canonical calendar date or UTC tick-precision instant spelling.");
    }

    internal static int Compare(SemanticValue left, SemanticValue right, SemanticPrimitiveType primitive)
    {
        if (left is not SemanticTextValue first || right is not SemanticTextValue second)
            throw new InvalidSemanticContract("A date comparison requires text values of its declared date primitive.");
        if (primitive == SemanticPrimitiveType.Date &&
            DateOnly.TryParseExact(first.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var firstDate) &&
            DateOnly.TryParseExact(second.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var secondDate))
        {
            return firstDate.CompareTo(secondDate);
        }
        if (primitive == SemanticPrimitiveType.DateTime && TryInstant(first.Value, out var firstInstant) && TryInstant(second.Value, out var secondInstant))
            return firstInstant.UtcTicks.CompareTo(secondInstant.UtcTicks);

        throw new InvalidSemanticContract("A date comparison requires valid dates or explicitly offset instants.");
    }

    internal static bool Satisfies(SemanticValidationRule rule, SemanticValue value, SemanticPrimitiveType primitive)
    {
        if (value is SemanticNullValue) return true;
        var comparison = Compare(value, rule.Operand!, primitive);

        return rule.Kind switch
        {
            SemanticValidationRuleKind.Equal => comparison == 0,
            SemanticValidationRuleKind.NotEqual => comparison != 0,
            SemanticValidationRuleKind.GreaterThan => comparison > 0,
            SemanticValidationRuleKind.GreaterThanOrEqual => comparison >= 0,
            SemanticValidationRuleKind.LessThan => comparison < 0,
            SemanticValidationRuleKind.LessThanOrEqual => comparison <= 0,
            _ => throw new InvalidSemanticContract("Unknown date comparison rule.")
        };
    }
}

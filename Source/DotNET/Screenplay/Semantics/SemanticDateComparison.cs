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

    internal static string? Canonical(string value, SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Date when DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        SemanticPrimitiveType.DateTime when SemanticDateComparisons.TryParseInstant(value, out var instant) => instant.UtcDateTime.ToString("O", CultureInfo.InvariantCulture),
        _ => null
    };

    internal static void ValidateConstant(SemanticValue? value, SemanticPrimitiveType primitive)
    {
        if (value is not SemanticTextValue text || Canonical(text.Value, primitive) != text.Value)
            throw new InvalidSemanticContract("A date comparison constant must use its canonical calendar date or UTC tick-precision instant spelling.");
    }
}

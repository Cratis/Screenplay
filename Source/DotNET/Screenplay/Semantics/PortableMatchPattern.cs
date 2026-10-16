// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Checks authored match patterns without constructing a platform regular expression.
/// </summary>
public static class PortableMatchPattern
{
    /// <summary>
    /// Checks whether a decoded pattern belongs to Screenplay's portable match dialect.
    /// </summary>
    /// <param name="pattern">The decoded pattern, without delimiters or flags.</param>
    /// <returns>The admission result, diagnostic details and optional equivalent text rule.</returns>
    public static PortableMatchCheck Check(string pattern)
    {
        if (LegacyTextRulePatterns.Recognize(pattern) is { } replacement)
        {
            var spelling = replacement.Kind switch
            {
                SemanticValidationRuleKind.Equal => "==",
                SemanticValidationRuleKind.NotEqual => "!=",
                SemanticValidationRuleKind.NotContains => "not contains",
                _ => "ends with"
            };
            var suggestion = $"Use '{spelling}' with the literal text operand instead.";

            return new(PortableMatchStatus.Deprecated, DiagnosticCodes.DeprecatedStudioTextRulePattern, "Studio text rule", 0, suggestion, $"This Studio lookaround pattern is deprecated. {suggestion}", replacement);
        }

        var parsed = SemanticMatchPattern.Parse(pattern);
        if (parsed.Accepted) return new(PortableMatchStatus.Admitted, null, null, 0, null, string.Empty);

        return new(
            parsed.Invalid ? PortableMatchStatus.Invalid : PortableMatchStatus.Refused,
            parsed.Invalid ? DiagnosticCodes.InvalidMatchPattern : DiagnosticCodes.PatternOutsidePortableMatchDialect,
            parsed.Construct,
            parsed.Offset,
            parsed.Suggestion,
            parsed.Description);
    }
}

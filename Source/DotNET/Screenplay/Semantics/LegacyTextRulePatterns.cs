// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Screenplay.Semantics;

internal static class LegacyTextRulePatterns
{
    const string StrictEnd = @"(?![\s\S])";
    const string V2Escaped = @"\^$.|?*+()[]{}/";
    const string V1Escaped = @"\*+?|{[()^$.# ";

    internal static PortableMatchReplacement? Recognize(string pattern)
    {
        if (pattern.Length >= 11 + 1 && pattern.StartsWith(@"^(?![\s\S]*", StringComparison.Ordinal) && pattern.EndsWith(')'))
        {
            var literal = pattern[11..^1];
            var value = Decode(literal, false) ?? Decode(literal, true);

            return value is null ? null : new(SemanticValidationRuleKind.NotContains, value);
        }

        if (pattern.StartsWith("^(?!", StringComparison.Ordinal))
        {
            if (pattern.Length >= 4 + StrictEnd.Length + 1 && pattern.EndsWith(StrictEnd + ")", StringComparison.Ordinal)) return Replacement(SemanticValidationRuleKind.NotEqual, pattern[4..^(StrictEnd.Length + 1)], false);
            if (pattern.Length >= 4 + 2 && pattern.EndsWith("$)", StringComparison.Ordinal)) return Replacement(SemanticValidationRuleKind.NotEqual, pattern[4..^2], true);
        }

        if (pattern.Length < StrictEnd.Length || !pattern.EndsWith(StrictEnd, StringComparison.Ordinal)) return null;
        if (pattern.Length >= 1 + StrictEnd.Length && pattern.StartsWith('^')) return Replacement(SemanticValidationRuleKind.Equal, pattern[1..^StrictEnd.Length], false);

        return Replacement(SemanticValidationRuleKind.EndsWith, pattern[..^StrictEnd.Length], false);
    }

    internal static SemanticValidationRule Normalize(SemanticValidationRule rule) =>
        rule is { Kind: SemanticValidationRuleKind.Matches, Operand: SemanticTextValue pattern } && Recognize(pattern.Value) is { } replacement
            ? rule with { Kind = replacement.Kind, Operand = SemanticValue.Text(replacement.Value) }
            : rule;

    // The v1 implementation is held to real Regex.Escape by a test-only oracle.
    internal static string Escape(string value, bool v1)
    {
        var escaped = new StringBuilder();
        foreach (var character in value)
        {
            if (v1 && character is '\t' or '\n' or '\f' or '\r')
            {
                escaped.Append('\\').Append(character switch { '\t' => 't', '\n' => 'n', '\f' => 'f', _ => 'r' });
            }
            else
            {
                if ((v1 ? V1Escaped : V2Escaped).Contains(character, StringComparison.Ordinal)) escaped.Append('\\');
                escaped.Append(character);
            }
        }

        return escaped.ToString();
    }

    static PortableMatchReplacement? Replacement(SemanticValidationRuleKind kind, string literal, bool v1) =>
        Decode(literal, v1) is { } value ? new(kind, value) : null;

    static string? Decode(string literal, bool v1)
    {
        var value = new StringBuilder();
        for (var index = 0; index < literal.Length; index++)
        {
            var character = literal[index];
            if (character == '\\')
            {
                if (++index == literal.Length) return null;
                character = literal[index];
                if (v1) character = character switch { 't' => '\t', 'n' => '\n', 'f' => '\f', 'r' => '\r', _ => character };
            }
            value.Append(character);
        }
        var decoded = value.ToString();

        return string.Equals(Escape(decoded, v1), literal, StringComparison.Ordinal) ? decoded : null;
    }
}

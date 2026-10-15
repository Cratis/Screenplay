// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Whether a template reference's <c>version</c> is a range Scene understands, matching Scene's
/// <c>isValidVersionRange</c> / <c>PackageVersionRange.IsValidRange</c>: empty or <c>*</c>, a bare or <c>=</c>-prefixed version,
/// or a <c>^</c>, <c>~</c>, <c>&gt;=</c>, <c>&gt;</c>, <c>&lt;=</c> or <c>&lt;</c> comparison with a <c>major[.minor[.patch]]</c> version
/// and an optional pre-release or build suffix. Whether the installed blueprint satisfies it is decided where the blueprint is known (Stage and Scene).
/// </summary>
internal static class TemplateVersionRange
{
    static readonly string[] _operators = [">=", "<=", "^", "~", ">", "<", "="];

    /// <summary>
    /// Checks a range.
    /// </summary>
    /// <param name="range">The range text.</param>
    /// <returns><see langword="true"/> when Scene can read the range.</returns>
    public static bool IsValid(string range)
    {
        var trimmed = range.Trim();
        if (trimmed.Length == 0 || trimmed == "*")
        {
            return true;
        }

        var literal = trimmed;
        foreach (var candidate in _operators)
        {
            if (trimmed.StartsWith(candidate, StringComparison.Ordinal))
            {
                literal = trimmed[candidate.Length..].Trim();
                break;
            }
        }

        var suffix = literal.IndexOfAny(['-', '+']);
        var numeric = (suffix >= 0 ? literal[..suffix] : literal).Trim();
        var parts = numeric.Split('.');
        return parts.Length is >= 1 and <= 3 && parts.All(part => part.Length > 0 && part.All(char.IsAsciiDigit));
    }
}

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

    /// <summary>
    /// Checks whether a version satisfies a range, matching Scene's <c>PackageVersionRange.IsSatisfiedBy</c>.
    /// </summary>
    /// <param name="version">The installed version.</param>
    /// <param name="range">The range; empty or <c>*</c> accepts every version.</param>
    /// <returns><see langword="true"/> when the version satisfies the range.</returns>
    public static bool IsSatisfiedBy(string version, string? range)
    {
        var trimmed = range?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed == "*")
        {
            return true;
        }

        var (op, literal) = Split(trimmed);
        if (!TryParse(version, out var actual) || !TryParse(literal, out var required))
        {
            return false;
        }

        return op switch
        {
            "^" => actual.CompareTo(required) >= 0 && actual.CompareTo(CaretUpperBound(required)) < 0,
            "~" => actual.CompareTo(required) >= 0 && actual.CompareTo((required.Major, required.Minor + 1, 0)) < 0,
            ">=" => actual.CompareTo(required) >= 0,
            ">" => actual.CompareTo(required) > 0,
            "<=" => actual.CompareTo(required) <= 0,
            "<" => actual.CompareTo(required) < 0,
            _ => actual.CompareTo(required) == 0
        };
    }

    static (string Operator, string Literal) Split(string range)
    {
        foreach (var candidate in _operators)
        {
            if (range.StartsWith(candidate, StringComparison.Ordinal))
            {
                return (candidate == "=" ? string.Empty : candidate, range[candidate.Length..].Trim());
            }
        }

        return (string.Empty, range);
    }

    static (int Major, int Minor, int Patch) CaretUpperBound((int Major, int Minor, int Patch) version) =>
        version switch
        {
            { Major: > 0 } => (version.Major + 1, 0, 0),
            { Minor: > 0 } => (0, version.Minor + 1, 0),
            _ => (0, 0, version.Patch + 1)
        };

    static bool TryParse(string value, out (int Major, int Minor, int Patch) version)
    {
        version = default;
        var numeric = value.Trim();
        var suffix = numeric.IndexOfAny(['-', '+']);
        if (suffix >= 0)
        {
            numeric = numeric[..suffix];
        }

        var parts = numeric.Split('.');
        if (parts.Length is 0 or > 3)
        {
            return false;
        }

        var components = new int[3];
        for (var index = 0; index < parts.Length; index++)
        {
            if (!int.TryParse(parts[index], out components[index]) || components[index] < 0)
            {
                return false;
            }
        }

        version = (components[0], components[1], components[2]);
        return true;
    }
}

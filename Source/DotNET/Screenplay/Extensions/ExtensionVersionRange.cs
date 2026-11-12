// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Extensions;

/// <summary>
/// Matches versions against the range an <c>extension</c> line asks for.
/// </summary>
/// <remarks>
/// <c>latest</c> matches every release; <c>2</c> the latest 2.x; <c>2.3</c> the latest 2.3.x; <c>2.3.1</c> exactly that release;
/// <c>^2.3</c> any version from 2.3 below 3; <c>~2.3</c> any version from 2.3 below 2.4.
/// </remarks>
public static class ExtensionVersionRange
{
    /// <summary>
    /// Determines whether a version satisfies a range.
    /// </summary>
    /// <param name="range">The range as the lock records it.</param>
    /// <param name="version">The version.</param>
    /// <returns><see langword="true"/> when the version satisfies the range.</returns>
    public static bool Satisfies(string range, string version)
    {
        if (!TryParse(version, out var candidate)) return false;
        if (range == ExtensionLock.LatestRange) return true;
        var caret = range.StartsWith('^');
        var tilde = range.StartsWith('~');
        var text = caret || tilde ? range[1..] : range;
        var written = text.Split('.');
        if (!TryParse(text, out var lower)) return false;
        if (caret) return Compare(candidate, lower) >= 0 && candidate[0] == lower[0] && (lower[0] > 0 || written.Length < 2 || candidate[1] == lower[1]);
        if (tilde) return Compare(candidate, lower) >= 0 && candidate[0] == lower[0] && (written.Length < 2 || candidate[1] == lower[1]);
        for (var index = 0; index < written.Length; index++) if (candidate[index] != lower[index]) return false;
        return true;
    }

    /// <summary>
    /// Compares two versions.
    /// </summary>
    /// <param name="left">The left version.</param>
    /// <param name="right">The right version.</param>
    /// <returns>A negative, zero or positive number.</returns>
    public static int Compare(string left, string right) => TryParse(left, out var a) && TryParse(right, out var b) ? Compare(a, b) : string.CompareOrdinal(left, right);

    static int Compare(int[] left, int[] right)
    {
        for (var index = 0; index < 3; index++)
        {
            if (left[index] != right[index]) return left[index].CompareTo(right[index]);
        }

        return 0;
    }

    static bool TryParse(string text, out int[] parts)
    {
        parts = [0, 0, 0];
        var pieces = text.Split('.');
        if (pieces.Length is < 1 or > 3) return false;
        for (var index = 0; index < pieces.Length; index++)
        {
            if (!int.TryParse(pieces[index], out parts[index]) || parts[index] < 0) return false;
        }

        return true;
    }
}

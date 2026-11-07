// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Represents a major version of the Screenplay language - the product major a breaking change ships in. A migration
/// moves a document from one to the next.
/// </summary>
/// <remarks>
/// This is not the executable semantic model's <see cref="Semantics.LanguageVersion"/>, which versions the model the
/// compiler produces. A Screenplay version is what an author writes against.
/// </remarks>
/// <param name="Major">The major version number.</param>
public readonly record struct ScreenplayVersion(int Major)
{
    /// <summary>
    /// Gets the Screenplay version this compiler reads: the version a migration ends at unless told otherwise.
    /// </summary>
    public static ScreenplayVersion Current { get; } = new(6);

    /// <summary>
    /// Converts a major version number to a <see cref="ScreenplayVersion"/>.
    /// </summary>
    /// <param name="major">The major version number.</param>
    public static implicit operator ScreenplayVersion(int major) => new(major);

    /// <summary>Compares two versions.</summary>
    /// <param name="left">The left version.</param>
    /// <param name="right">The right version.</param>
    /// <returns><c>true</c> when <paramref name="left"/> is earlier.</returns>
    public static bool operator <(ScreenplayVersion left, ScreenplayVersion right) => left.Major < right.Major;

    /// <summary>Compares two versions.</summary>
    /// <param name="left">The left version.</param>
    /// <param name="right">The right version.</param>
    /// <returns><c>true</c> when <paramref name="left"/> is later.</returns>
    public static bool operator >(ScreenplayVersion left, ScreenplayVersion right) => left.Major > right.Major;

    /// <summary>Compares two versions.</summary>
    /// <param name="left">The left version.</param>
    /// <param name="right">The right version.</param>
    /// <returns><c>true</c> when <paramref name="left"/> is earlier or the same.</returns>
    public static bool operator <=(ScreenplayVersion left, ScreenplayVersion right) => left.Major <= right.Major;

    /// <summary>Compares two versions.</summary>
    /// <param name="left">The left version.</param>
    /// <param name="right">The right version.</param>
    /// <returns><c>true</c> when <paramref name="left"/> is later or the same.</returns>
    public static bool operator >=(ScreenplayVersion left, ScreenplayVersion right) => left.Major >= right.Major;

    /// <summary>
    /// Reads a version as an author writes it - <c>5</c>, <c>5.0</c> or <c>v5.11.0</c>; only the major counts.
    /// </summary>
    /// <param name="text">The text to read.</param>
    /// <param name="version">The version read.</param>
    /// <returns><c>true</c> when the text names a version.</returns>
    public static bool TryParse(string? text, out ScreenplayVersion version)
    {
        version = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed[0] is 'v' or 'V')
        {
            trimmed = trimmed[1..];
        }

        var major = trimmed.Split('.')[0];
        if (!int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 1)
        {
            return false;
        }

        version = new(number);
        return true;
    }

    /// <inheritdoc/>
    public override string ToString() => Major.ToString(CultureInfo.InvariantCulture);
}

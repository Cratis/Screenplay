// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents the version of the Screenplay source language.
/// </summary>
/// <param name="Major">The major version.</param>
/// <param name="Minor">The minor version.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct LanguageVersion(uint Major, uint Minor) : ISpanFormattable
{
    /// <summary>
    /// Gets the initial language version.
    /// </summary>
    public static readonly LanguageVersion V1 = new(1, 0);

    /// <summary>
    /// Parses a canonical, supported language version.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed version.</returns>
    /// <exception cref="InvalidSemanticContract">The value is not canonical or supported by ESM schema v1.</exception>
    public static LanguageVersion Parse(string value) => VersionParser.ParseLanguage(value);

    /// <summary>
    /// Tries to parse a canonical, supported language version.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="version">The parsed version when successful.</param>
    /// <returns><c>true</c> when the value is canonical and supported; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out LanguageVersion version) => VersionParser.TryParse(value, out version);

    /// <inheritdoc/>
    public override string ToString() => string.Concat(
        Major.ToString(CultureInfo.InvariantCulture),
        ".",
        Minor.ToString(CultureInfo.InvariantCulture));

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        destination.TryWrite(CultureInfo.InvariantCulture, $"{Major}.{Minor}", out charsWritten);
}

/// <summary>
/// Represents the version of portable Screenplay execution semantics.
/// </summary>
/// <param name="Major">The major version.</param>
/// <param name="Minor">The minor version.</param>
[StructLayout(LayoutKind.Auto)]
public readonly record struct SemanticVersion(uint Major, uint Minor) : ISpanFormattable
{
    /// <summary>
    /// Gets the initial semantic version.
    /// </summary>
    public static readonly SemanticVersion V1 = new(1, 0);

    /// <summary>
    /// Parses a canonical, supported semantic version.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <returns>The parsed version.</returns>
    /// <exception cref="InvalidSemanticContract">The value is not canonical or supported by ESM schema v1.</exception>
    public static SemanticVersion Parse(string value) => VersionParser.ParseSemantic(value);

    /// <summary>
    /// Tries to parse a canonical, supported semantic version.
    /// </summary>
    /// <param name="value">The value to parse.</param>
    /// <param name="version">The parsed version when successful.</param>
    /// <returns><c>true</c> when the value is canonical and supported; otherwise, <c>false</c>.</returns>
    public static bool TryParse(string? value, out SemanticVersion version) => VersionParser.TryParse(value, out version);

    /// <inheritdoc/>
    public override string ToString() => string.Concat(
        Major.ToString(CultureInfo.InvariantCulture),
        ".",
        Minor.ToString(CultureInfo.InvariantCulture));

    /// <inheritdoc/>
    public string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    /// <inheritdoc/>
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
        destination.TryWrite(CultureInfo.InvariantCulture, $"{Major}.{Minor}", out charsWritten);
}

/// <summary>
/// Defines the language and semantic versions admitted by ESM schema v1.
/// </summary>
public static class EsmSchemaV1Support
{
    /// <summary>
    /// Gets the supported source language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [LanguageVersion.V1];

    /// <summary>
    /// Gets the supported portable semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [SemanticVersion.V1];

    /// <summary>
    /// Determines whether a language version is supported by ESM schema v1.
    /// </summary>
    /// <param name="version">The language version.</param>
    /// <returns><c>true</c> when supported; otherwise, <c>false</c>.</returns>
    public static bool Supports(LanguageVersion version) => LanguageVersions.Contains(version);

    /// <summary>
    /// Determines whether a semantic version is supported by ESM schema v1.
    /// </summary>
    /// <param name="version">The semantic version.</param>
    /// <returns><c>true</c> when supported; otherwise, <c>false</c>.</returns>
    public static bool Supports(SemanticVersion version) => SemanticVersions.Contains(version);

    /// <summary>
    /// Rejects a language and semantic version pair not supported by ESM schema v1.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <exception cref="InvalidSemanticContract">One or both versions are unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion) || !Supports(semanticVersion))
        {
            throw new InvalidSemanticContract(
                $"ESM schema v1 supports language version '{LanguageVersion.V1}' and semantic version '{SemanticVersion.V1}' only; received '{languageVersion}' and '{semanticVersion}'.");
        }
    }
}

static class VersionParser
{
    internal static LanguageVersion ParseLanguage(string value)
    {
        if (!TryParse(value, out LanguageVersion version))
        {
            throw new InvalidSemanticContract($"'{value}' is not a canonical language version supported by ESM schema v1.");
        }

        return version;
    }

    internal static SemanticVersion ParseSemantic(string value)
    {
        if (!TryParse(value, out SemanticVersion version))
        {
            throw new InvalidSemanticContract($"'{value}' is not a canonical semantic version supported by ESM schema v1.");
        }

        return version;
    }

    internal static bool TryParse(string? value, out LanguageVersion version)
    {
        var success = TryParseParts(value, out var major, out var minor);
        version = success ? new(major, minor) : default;
        if (success && !EsmSchemaV1Support.Supports(version))
        {
            version = default;
            return false;
        }

        return success;
    }

    internal static bool TryParse(string? value, out SemanticVersion version)
    {
        var success = TryParseParts(value, out var major, out var minor);
        version = success ? new(major, minor) : default;
        if (success && !EsmSchemaV1Support.Supports(version))
        {
            version = default;
            return false;
        }

        return success;
    }

    static bool TryParseParts(string? value, out uint major, out uint minor)
    {
        major = 0;
        minor = 0;
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        var separator = value.IndexOf('.');
        if (separator <= 0 || separator != value.LastIndexOf('.') || separator == value.Length - 1)
        {
            return false;
        }

        var majorText = value.AsSpan(0, separator);
        var minorText = value.AsSpan(separator + 1);
        if ((majorText.Length > 1 && majorText[0] == '0') || (minorText.Length > 1 && minorText[0] == '0'))
        {
            return false;
        }

        return uint.TryParse(majorText, NumberStyles.None, CultureInfo.InvariantCulture, out major) &&
               uint.TryParse(minorText, NumberStyles.None, CultureInfo.InvariantCulture, out minor) &&
               major > 0;
    }
}

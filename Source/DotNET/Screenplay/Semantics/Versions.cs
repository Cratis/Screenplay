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
    /// Gets the language version reserved for explicit specification event-source assertions and event-context semantics.
    /// </summary>
    public static readonly LanguageVersion V2 = new(2, 0);

    /// <summary>The language version for opaque reducer transition contracts.</summary>
    public static readonly LanguageVersion V3 = new(3, 0);

    /// <summary>The language version for event-contract lineage.</summary>
    public static readonly LanguageVersion V4 = new(4, 0);

    /// <summary>The language version for keyed read-model absence assertions.</summary>
    public static readonly LanguageVersion V5 = new(5, 0);

    /// <summary>The language version for reactions, captures, application triggers and the clock.</summary>
    public static readonly LanguageVersion V6 = new(6, 0);

    /// <summary>
    /// The language version for generated command values, responses and policy negation.
    /// </summary>
    public static readonly LanguageVersion V7 = new(7, 0);

    /// <summary>
    /// The language version for event sources, streams, command routes and specification routes.
    /// </summary>
    public static readonly LanguageVersion V8 = new(8, 0);

    /// <summary>
    /// The language version for public events, translation direction, event-target projections and reducers, and event-source captures.
    /// </summary>
    public static readonly LanguageVersion V9 = new(9, 0);

    /// <summary>
    /// The language version for reaction system identity, production routes and observer filters.
    /// </summary>
    public static readonly LanguageVersion V10 = new(10, 0);

    /// <summary>
    /// The language version for exact Double-mode number literal lowering.
    /// </summary>
    public static readonly LanguageVersion V11 = new(11, 0);

    /// <summary>
    /// The language version for non-positional enum member numbers.
    /// </summary>
    public static readonly LanguageVersion V12 = new(12, 0);

    /// <summary>
    /// The language version for concept compliance markers.
    /// </summary>
    public static readonly LanguageVersion V13 = new(13, 0);

    /// <summary>
    /// The language version for caller-supplied equality query filters.
    /// </summary>
    public static readonly LanguageVersion V14 = new(14, 0);

    /// <summary>
    /// The language version for concept generation strategies and guarded freshness.
    /// </summary>
    public static readonly LanguageVersion V15 = new(15, 0);

    /// <summary>
    /// The language version for stored event type names.
    /// </summary>
    public static readonly LanguageVersion V16 = new(16, 0);

    /// <summary>
    /// The language version for explicit no-event assertions and recovery redelivery.
    /// </summary>
    public static readonly LanguageVersion V17 = new(17, 0);

    /// <summary>
    /// Gets version 18.0, admitting query validation rejection specifications.
    /// </summary>
    public static readonly LanguageVersion V18 = new(18, 0);

    /// <summary>
    /// The language version for reaction refusal branches and refusal values.
    /// </summary>
    public static readonly LanguageVersion V19 = new(19, 0);

    /// <summary>
    /// The language version for rules on generated concepts.
    /// </summary>
    public static readonly LanguageVersion V20 = new(20, 0);

    /// <summary>
    /// Gets phase-3 generation language version 21.0.
    /// </summary>
    public static readonly LanguageVersion V21 = new(21, 0);

    /// <summary>
    /// Gets atomic reaction production language version 22.0.
    /// </summary>
    public static readonly LanguageVersion V22 = new(22, 0);

    /// <summary>
    /// Described command handlers.
    /// </summary>
    public static readonly LanguageVersion V23 = new(23, 0);

    /// <summary>
    /// First-class ordinal text rules.
    /// </summary>
    public static readonly LanguageVersion V24 = new(TextRuleVersion.Major, 0);

    /// <summary>
    /// Gets protected command reads language version 25.0.
    /// </summary>
    public static readonly LanguageVersion V25 = new(25, 0);

    /// <summary>
    /// Gets chronological command date comparison language version 26.0.
    /// </summary>
    public static readonly LanguageVersion V26 = new(26, 0);

    /// <summary>
    /// Gets the language version admitting flat composite read-model keys.
    /// </summary>
    public static readonly LanguageVersion V27 = new(CompositeReadModelKeyVersion.Major, 0);

    /// <summary>
    /// Gets the language version admitting command Uuid equality.
    /// </summary>
    public static readonly LanguageVersion V28 = new(28, 0);

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
    /// Gets the semantic version reserved for typed state-change destinations and event occurrence context.
    /// </summary>
    public static readonly SemanticVersion V2 = new(2, 0);

    /// <summary>The semantic version for opaque reducer transition contracts.</summary>
    public static readonly SemanticVersion V3 = new(3, 0);

    /// <summary>The semantic version for event-contract lineage.</summary>
    public static readonly SemanticVersion V4 = new(4, 0);

    /// <summary>The semantic version for keyed read-model absence assertions.</summary>
    public static readonly SemanticVersion V5 = new(5, 0);

    /// <summary>The semantic version for reactions, captures, application triggers and the clock.</summary>
    public static readonly SemanticVersion V6 = new(6, 0);

    /// <summary>
    /// The semantic version for generated command values, responses and policy negation.
    /// </summary>
    public static readonly SemanticVersion V7 = new(7, 0);

    /// <summary>
    /// The semantic version for event sources, streams, command routes and specification routes.
    /// </summary>
    public static readonly SemanticVersion V8 = new(8, 0);

    /// <summary>
    /// The semantic version for public events, translation direction, event-target projections and reducers, and event-source captures.
    /// </summary>
    public static readonly SemanticVersion V9 = new(9, 0);

    /// <summary>
    /// The semantic version for reaction system identity, production routes and observer filters.
    /// </summary>
    public static readonly SemanticVersion V10 = new(10, 0);

    /// <summary>
    /// The semantic version for exact Double-mode number literal lowering.
    /// </summary>
    public static readonly SemanticVersion V11 = new(11, 0);

    /// <summary>
    /// The semantic version for non-positional enum member numbers.
    /// </summary>
    public static readonly SemanticVersion V12 = new(12, 0);

    /// <summary>
    /// The semantic version for concept compliance markers.
    /// </summary>
    public static readonly SemanticVersion V13 = new(13, 0);

    /// <summary>
    /// The semantic version for caller-supplied equality query filters.
    /// </summary>
    public static readonly SemanticVersion V14 = new(14, 0);

    /// <summary>
    /// The semantic version for concept generation strategies and guarded freshness.
    /// </summary>
    public static readonly SemanticVersion V15 = new(15, 0);

    /// <summary>
    /// The semantic version for stored event type names.
    /// </summary>
    public static readonly SemanticVersion V16 = new(16, 0);

    /// <summary>
    /// The semantic version for explicit no-event assertions and recovery redelivery.
    /// </summary>
    public static readonly SemanticVersion V17 = new(17, 0);

    /// <summary>
    /// Gets version 18.0, admitting query validation rejection specifications.
    /// </summary>
    public static readonly SemanticVersion V18 = new(18, 0);

    /// <summary>
    /// The semantic version for reaction refusal branches and refusal values.
    /// </summary>
    public static readonly SemanticVersion V19 = new(19, 0);

    /// <summary>
    /// The semantic version for rules on generated concepts.
    /// </summary>
    public static readonly SemanticVersion V20 = new(20, 0);

    /// <summary>
    /// Gets phase-3 generation semantic version 21.0.
    /// </summary>
    public static readonly SemanticVersion V21 = new(21, 0);

    /// <summary>
    /// Gets atomic reaction production semantic version 22.0.
    /// </summary>
    public static readonly SemanticVersion V22 = new(22, 0);

    /// <summary>
    /// Described command handlers.
    /// </summary>
    public static readonly SemanticVersion V23 = new(23, 0);

    /// <summary>
    /// First-class ordinal text rules.
    /// </summary>
    public static readonly SemanticVersion V24 = new(TextRuleVersion.Major, 0);

    /// <summary>
    /// Gets protected command reads semantic version 25.0.
    /// </summary>
    public static readonly SemanticVersion V25 = new(25, 0);

    /// <summary>
    /// Gets chronological command date comparison semantic version 26.0.
    /// </summary>
    public static readonly SemanticVersion V26 = new(26, 0);

    /// <summary>
    /// Gets the semantic version admitting flat composite read-model keys.
    /// </summary>
    public static readonly SemanticVersion V27 = new(CompositeReadModelKeyVersion.Major, 0);

    /// <summary>
    /// Gets the semantic version admitting command Uuid equality.
    /// </summary>
    public static readonly SemanticVersion V28 = new(28, 0);

    /// <summary>
    /// Gets whether this version admits everything another version admits - each version builds on the ones before it.
    /// </summary>
    /// <param name="minimum">The <see cref="SemanticVersion"/> whose constructs must be admitted.</param>
    /// <returns><c>true</c> when this version is the same as or later than <paramref name="minimum"/>.</returns>
    public bool IsAtLeast(SemanticVersion minimum) => Major > minimum.Major || (Major == minimum.Major && Minor >= minimum.Minor);

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

/// <summary>
/// Defines language and semantic version pairs reserved by the ESM schema-v2 contract.
/// </summary>
public static class EsmSchemaV2Support
{
    /// <summary>
    /// Gets the source language versions declared by the schema-v2 contract.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [LanguageVersion.V1, LanguageVersion.V2];

    /// <summary>
    /// Gets the portable semantic versions declared by the schema-v2 contract.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [SemanticVersion.V1, SemanticVersion.V2];

    /// <summary>
    /// Determines whether a language version is known to schema v2.
    /// </summary>
    /// <param name="version">The language version.</param>
    /// <returns><see langword="true"/> when known; otherwise, <see langword="false"/>.</returns>
    public static bool Supports(LanguageVersion version) => LanguageVersions.Contains(version);

    /// <summary>
    /// Determines whether a semantic version is known to schema v2.
    /// </summary>
    /// <param name="version">The semantic version.</param>
    /// <returns><see langword="true"/> when known; otherwise, <see langword="false"/>.</returns>
    public static bool Supports(SemanticVersion version) => SemanticVersions.Contains(version);

    /// <summary>
    /// Determines whether one exact language and semantic version pair has defined schema-v2 meaning.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <returns><see langword="true"/> when the pair has defined meaning; otherwise, <see langword="false"/>.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        (languageVersion == LanguageVersion.V1 && semanticVersion == SemanticVersion.V1) ||
        (languageVersion == LanguageVersion.V2 && semanticVersion == SemanticVersion.V2);

    /// <summary>
    /// Rejects a version pair not declared by the ESM schema-v2 contract.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <exception cref="InvalidSemanticContract">One or both versions are unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract(
                $"The ESM schema-v2 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>Defines version pairs admitted by ESM schema v3.</summary>
public static class EsmSchemaV3Support
{
    /// <summary>Gets the supported language versions.</summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [LanguageVersion.V1, LanguageVersion.V2, LanguageVersion.V3];

    /// <summary>Gets the supported semantic versions.</summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [SemanticVersion.V1, SemanticVersion.V2, SemanticVersion.V3];

    /// <summary>Determines whether a version pair has defined meaning.</summary>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV2Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V3 && semanticVersion == SemanticVersion.V3);

    /// <summary>Rejects unsupported version pairs.</summary>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v3 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>Defines version pairs admitted by ESM schema v4.</summary>
public static class EsmSchemaV4Support
{
    /// <summary>Gets supported language versions.</summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV3Support.LanguageVersions, LanguageVersion.V4];

    /// <summary>Gets supported semantic versions.</summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV3Support.SemanticVersions, SemanticVersion.V4];

    /// <summary>Determines whether the exact pair is supported.</summary>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV3Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V4 && semanticVersion == SemanticVersion.V4);

    /// <summary>Rejects unsupported version pairs.</summary>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v4 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>Defines version pairs admitted by ESM schema v5.</summary>
public static class EsmSchemaV5Support
{
    /// <summary>Gets the supported language versions.</summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV4Support.LanguageVersions, LanguageVersion.V5];

    /// <summary>Gets the supported semantic versions.</summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV4Support.SemanticVersions, SemanticVersion.V5];

    /// <summary>Determines whether the exact pair is supported.</summary>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV4Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V5 && semanticVersion == SemanticVersion.V5);

    /// <summary>Rejects unsupported version pairs.</summary>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v5 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>Defines version pairs admitted by ESM schema v6.</summary>
public static class EsmSchemaV6Support
{
    /// <summary>Gets the supported language versions.</summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV5Support.LanguageVersions, LanguageVersion.V6];

    /// <summary>Gets the supported semantic versions.</summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV5Support.SemanticVersions, SemanticVersion.V6];

    /// <summary>Determines whether the exact pair is supported.</summary>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV5Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V6 && semanticVersion == SemanticVersion.V6);

    /// <summary>Rejects unsupported version pairs.</summary>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v6 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v7.
/// </summary>
public static class EsmSchemaV7Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV6Support.LanguageVersions, LanguageVersion.V7];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV6Support.SemanticVersions, SemanticVersion.V7];

    /// <summary>
    /// Determines whether the exact pair is supported.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV6Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V7 && semanticVersion == SemanticVersion.V7);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v7 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v8.
/// </summary>
public static class EsmSchemaV8Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV7Support.LanguageVersions, LanguageVersion.V8];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV7Support.SemanticVersions, SemanticVersion.V8];

    /// <summary>
    /// Determines whether the exact pair is supported.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV7Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V8 && semanticVersion == SemanticVersion.V8);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v8 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v9.
/// </summary>
public static class EsmSchemaV9Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV8Support.LanguageVersions, LanguageVersion.V9];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV8Support.SemanticVersions, SemanticVersion.V9];

    /// <summary>
    /// Determines whether the exact pair is supported.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV8Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V9 && semanticVersion == SemanticVersion.V9);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v9 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v10.
/// </summary>
public static class EsmSchemaV10Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV9Support.LanguageVersions, LanguageVersion.V10];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV9Support.SemanticVersions, SemanticVersion.V10];

    /// <summary>
    /// Determines whether the exact pair is supported.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV9Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V10 && semanticVersion == SemanticVersion.V10);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v10 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v11.
/// </summary>
public static class EsmSchemaV11Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV10Support.LanguageVersions, LanguageVersion.V11];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV10Support.SemanticVersions, SemanticVersion.V11];

    /// <summary>
    /// Determines whether the exact pair is supported.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV10Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V11 && semanticVersion == SemanticVersion.V11);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v11 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v12.
/// </summary>
public static class EsmSchemaV12Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV11Support.LanguageVersions, LanguageVersion.V12];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV11Support.SemanticVersions, SemanticVersion.V12];

    /// <summary>
    /// Determines whether the exact pair is supported.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV11Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V12 && semanticVersion == SemanticVersion.V12);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v12 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v13.
/// </summary>
public static class EsmSchemaV13Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV12Support.LanguageVersions, LanguageVersion.V13];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV12Support.SemanticVersions, SemanticVersion.V13];

    /// <summary>
    /// Determines whether the exact pair is supported.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV12Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V13 && semanticVersion == SemanticVersion.V13);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v13 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v14.
/// </summary>
public static class EsmSchemaV14Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV13Support.LanguageVersions, LanguageVersion.V14];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV13Support.SemanticVersions, SemanticVersion.V14];

    /// <summary>
    /// Determines whether the exact pair is supported.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV13Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V14 && semanticVersion == SemanticVersion.V14);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v14 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v15.
/// </summary>
public static class EsmSchemaV15Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV14Support.LanguageVersions, LanguageVersion.V15];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV14Support.SemanticVersions, SemanticVersion.V15];

    /// <summary>
    /// Determines whether the exact version pair is supported.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV14Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V15 && semanticVersion == SemanticVersion.V15);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v15 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v16.
/// </summary>
public static class EsmSchemaV16Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV15Support.LanguageVersions, LanguageVersion.V16];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV15Support.SemanticVersions, SemanticVersion.V16];

    /// <summary>
    /// Determines whether the exact version pair is supported.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV15Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V16 && semanticVersion == SemanticVersion.V16);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v16 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v17.
/// </summary>
public static class EsmSchemaV17Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV16Support.LanguageVersions, LanguageVersion.V17];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV16Support.SemanticVersions, SemanticVersion.V17];

    /// <summary>
    /// Determines whether the exact version pair is supported.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV16Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V17 && semanticVersion == SemanticVersion.V17);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v17 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v18.
/// </summary>
public static class EsmSchemaV18Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV17Support.LanguageVersions, LanguageVersion.V18];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV17Support.SemanticVersions, SemanticVersion.V18];

    /// <summary>
    /// Determines whether the exact version pair is supported.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV17Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V18 && semanticVersion == SemanticVersion.V18);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v18 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v19.
/// </summary>
public static class EsmSchemaV19Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV18Support.LanguageVersions, LanguageVersion.V19];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV18Support.SemanticVersions, SemanticVersion.V19];

    /// <summary>
    /// Determines whether the exact version pair is supported.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV18Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V19 && semanticVersion == SemanticVersion.V19);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v19 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

/// <summary>
/// Defines version pairs admitted by ESM schema v20.
/// </summary>
public static class EsmSchemaV20Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV19Support.LanguageVersions, LanguageVersion.V20];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV19Support.SemanticVersions, SemanticVersion.V20];

    /// <summary>
    /// Determines whether the exact version pair is supported.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns>Whether the pair has defined meaning.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV19Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V20 && semanticVersion == SemanticVersion.V20);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"The ESM schema-v20 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
        }
    }
}

static class VersionParser
{
    internal static LanguageVersion ParseLanguage(string value)
    {
        if (!TryParse(value, out LanguageVersion version))
        {
            throw new InvalidSemanticContract($"'{value}' is not a canonical language version supported by ESM schema v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20 or v21.");
        }

        return version;
    }

    internal static SemanticVersion ParseSemantic(string value)
    {
        if (!TryParse(value, out SemanticVersion version))
        {
            throw new InvalidSemanticContract($"'{value}' is not a canonical semantic version supported by ESM schema v1, v2, v3, v4, v5, v6, v7, v8, v9, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19, v20 or v21.");
        }

        return version;
    }

    internal static bool TryParse(string? value, out LanguageVersion version)
    {
        var success = TryParseParts(value, out var major, out var minor);
        version = success ? new(major, minor) : default;
        if (success && !EsmSchemaV28Support.LanguageVersions.Contains(version))
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
        if (success && !EsmSchemaV28Support.SemanticVersions.Contains(version))
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

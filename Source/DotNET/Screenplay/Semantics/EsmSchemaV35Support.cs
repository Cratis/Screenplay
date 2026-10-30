// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the handler result contract v2 version pairs (ESM v35).
/// </summary>
public static class EsmSchemaV35Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV34Support.LanguageVersions, LanguageVersion.V35];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV34Support.SemanticVersions, SemanticVersion.V35];

    /// <summary>
    /// Determines whether the exact version pair has defined meaning.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns>Whether the pair is supported.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV34Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V35 && semanticVersion == SemanticVersion.V35);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion)) throw new InvalidSemanticContract($"The ESM schema-v35 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
    }
}

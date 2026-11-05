// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the application calendar and clock operand version pairs (ESM v37).
/// </summary>
public static class EsmSchemaV37Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV36Support.LanguageVersions, LanguageVersion.V37, LanguageVersion.V37_1];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV36Support.SemanticVersions, SemanticVersion.V37, SemanticVersion.V37_1];

    /// <summary>
    /// Determines whether the exact version pair has defined meaning.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns>Whether the pair is supported.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV36Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V37 && semanticVersion == SemanticVersion.V37) ||
        (languageVersion == LanguageVersion.V37_1 && semanticVersion == SemanticVersion.V37_1);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion)) throw new InvalidSemanticContract($"The ESM schema-v37 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
    }
}

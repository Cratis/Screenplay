// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the state machine and workflow version pairs (ESM v36).
/// </summary>
public static class EsmSchemaV36Support
{
    /// <summary>
    /// Gets the supported language versions.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV35Support.LanguageVersions, LanguageVersion.V36, LanguageVersion.V36_1];

    /// <summary>
    /// Gets the supported semantic versions.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV35Support.SemanticVersions, SemanticVersion.V36, SemanticVersion.V36_1];

    /// <summary>
    /// Determines whether the exact version pair has defined meaning.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns>Whether the pair is supported.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV35Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V36 && semanticVersion == SemanticVersion.V36) ||
        (languageVersion == LanguageVersion.V36_1 && semanticVersion == SemanticVersion.V36_1);

    /// <summary>
    /// Rejects unsupported version pairs.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <exception cref="InvalidSemanticContract">The version pair is unsupported.</exception>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion)) throw new InvalidSemanticContract($"The ESM schema-v36 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
    }
}

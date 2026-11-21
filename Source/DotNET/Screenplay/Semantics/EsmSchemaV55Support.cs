// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Declares the language and semantic version pairs of ESM schema v55, which adds exact ordered tag expectations on then events.
/// </summary>
public static class EsmSchemaV55Support
{
    /// <summary>
    /// Gets the language versions the schema declares.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV54Support.LanguageVersions, LanguageVersion.V55];

    /// <summary>
    /// Gets the semantic versions the schema declares.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV54Support.SemanticVersions, SemanticVersion.V55];

    /// <summary>
    /// Gets whether the schema declares a pair of versions.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns><c>true</c> when the pair is declared.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV54Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V55 && semanticVersion == SemanticVersion.V55);

    /// <summary>
    /// Throws when the schema does not declare a pair of versions.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion)) throw new InvalidSemanticContract($"The ESM schema-v55 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
    }
}

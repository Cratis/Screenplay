// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

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
    /// Determines whether the exact version pair is supported.
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

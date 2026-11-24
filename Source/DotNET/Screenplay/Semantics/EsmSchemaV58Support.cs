// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Declares the language and semantic version pairs of ESM schema v58, which adds graph projection declaring slice identities.
/// </summary>
public static class EsmSchemaV58Support
{
    /// <summary>
    /// Gets the language versions the schema declares.
    /// </summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = [.. EsmSchemaV57Support.LanguageVersions, LanguageVersion.V58];

    /// <summary>
    /// Gets the semantic versions the schema declares.
    /// </summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = [.. EsmSchemaV57Support.SemanticVersions, SemanticVersion.V58];

    /// <summary>
    /// Gets whether the schema declares a pair of versions.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns><c>true</c> when the pair is declared.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV57Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V58 && semanticVersion == SemanticVersion.V58);

    internal static (LanguageVersion Language, SemanticVersion Semantic)? SelectBoundVersions(SemanticApplication application)
    {
        if (SemanticModelValidator.UsesGraphProjectionDeclaringSlices(application)) return (LanguageVersion.V58, SemanticVersion.V58);
        if (SemanticModelValidator.UsesCodeProvides(application)) return (LanguageVersion.V57, SemanticVersion.V57);
        if (SemanticModelValidator.UsesFreeScreens(application)) return (LanguageVersion.V56, SemanticVersion.V56);
        if (SemanticModelValidator.UsesExpectedTags(application)) return (LanguageVersion.V55, SemanticVersion.V55);
        if (SemanticModelValidator.UsesGraphCollects(application)) return (LanguageVersion.V54, SemanticVersion.V54);
        return SemanticModelValidator.UsesAvailability(application) ? (LanguageVersion.V53, SemanticVersion.V53) : null;
    }

    /// <summary>
    /// Throws when the schema does not declare a pair of versions.
    /// </summary>
    /// <param name="languageVersion">The language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion)) throw new InvalidSemanticContract($"The ESM schema-v58 contract does not declare language version '{languageVersion}' and semantic version '{semanticVersion}'.");
    }
}

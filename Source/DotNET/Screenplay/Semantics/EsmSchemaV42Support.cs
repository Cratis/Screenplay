// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>Defines the versions admitted by ESM schema v42, with explicit agent exposure metadata.</summary>
public static class EsmSchemaV42Support
{
    /// <summary>Gets the supported source language versions.</summary>
    public static ImmutableArray<LanguageVersion> LanguageVersions { get; } = EsmSchemaV41Support.LanguageVersions.Add(LanguageVersion.V42);

    /// <summary>Gets the supported portable semantic versions.</summary>
    public static ImmutableArray<SemanticVersion> SemanticVersions { get; } = EsmSchemaV41Support.SemanticVersions.Add(SemanticVersion.V42);

    /// <summary>Determines whether the version pair is supported.</summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The semantic version.</param>
    /// <returns>Whether the pair is supported.</returns>
    public static bool Supports(LanguageVersion languageVersion, SemanticVersion semanticVersion) =>
        EsmSchemaV41Support.Supports(languageVersion, semanticVersion) ||
        (languageVersion == LanguageVersion.V42 && semanticVersion == SemanticVersion.V42);

    /// <summary>Rejects an unsupported version pair.</summary>
    /// <param name="languageVersion">The source language version.</param>
    /// <param name="semanticVersion">The portable semantic version.</param>
    public static void EnsureSupported(LanguageVersion languageVersion, SemanticVersion semanticVersion)
    {
        if (!Supports(languageVersion, semanticVersion))
        {
            throw new InvalidSemanticContract($"ESM schema v42 requires matching supported language and semantic versions; received '{languageVersion}' and '{semanticVersion}'.");
        }
    }
}

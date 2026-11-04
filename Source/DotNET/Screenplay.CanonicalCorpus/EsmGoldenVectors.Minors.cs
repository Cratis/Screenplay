// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalCorpus;

public static partial class EsmGoldenVectors
{
    /// <summary>
    /// Gets the exact nonzero minor versions, separately from the unchanged integer <see cref="Versions"/>.
    /// </summary>
    public static ImmutableArray<SemanticVersion> MinorVersions { get; } = [.. typeof(EsmGoldenVectors).Assembly.GetManifestResourceNames()
        .Where(name => name.StartsWith(FullPrefix, StringComparison.Ordinal) && name.EndsWith(Extension, StringComparison.Ordinal))
        .Select(name => name.Substring(FullPrefix.Length, name.Length - FullPrefix.Length - Extension.Length))
        .Where(version => version.Contains('.'))
        .Select(SemanticVersion.Parse)
        .OrderBy(version => version.Major).ThenBy(version => version.Minor)];

    /// <summary>
    /// Reads an exact nonzero minor golden without reserializing its bytes.
    /// </summary>
    /// <param name="version">A version listed in <see cref="MinorVersions"/>.</param>
    /// <returns>The exact canonical bytes and verified revision.</returns>
    /// <exception cref="InvalidSemanticContract">The version is absent or its embedded bytes are invalid.</exception>
    public static EsmMinorGoldenVector GetMinor(SemanticVersion version)
    {
        if (!MinorVersions.Contains(version)) throw new InvalidSemanticContract($"No full ESM minor golden is embedded for version '{version}'.");
        var vector = Read((int)version.Major, $"full-esm-v{version}.json");
        return new(version, vector.Bytes, vector.SemanticRevision);
    }
}

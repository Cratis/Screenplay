// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides the exact version-pinned ESM golden bytes embedded in the released corpus package.
/// </summary>
public static class EsmGoldenVectors
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Esm.";
    const string FullPrefix = $"{Prefix}full-esm-v";
    const string Extension = ".json";

    /// <summary>
    /// Gets full ESM vector versions in ascending order, discovered from the embedded resources.
    /// New full ESM golden files are included automatically when the package is built.
    /// </summary>
    public static ImmutableArray<int> Versions { get; } = [.. typeof(EsmGoldenVectors).Assembly.GetManifestResourceNames()
        .Where(name => name.StartsWith(FullPrefix, StringComparison.Ordinal) && name.EndsWith(Extension, StringComparison.Ordinal))
        .Select(name => int.Parse(name.AsSpan(FullPrefix.Length, name.Length - FullPrefix.Length - Extension.Length), CultureInfo.InvariantCulture))
        .Order()];

    /// <summary>
    /// Gets the separate byte-preserving ESM v7 policy-negation extension vector.
    /// </summary>
    /// <exception cref="InvalidSemanticContract">The embedded extension is missing or violates the ESM contract.</exception>
    public static EsmGoldenVector PolicyNegationV7 => Read(7, "policy-negation-v7.json");

    /// <summary>
    /// Reads the full golden vector for an ESM version without reserializing its bytes.
    /// </summary>
    /// <param name="version">The ESM version listed in <see cref="Versions"/>.</param>
    /// <returns>The version's exact golden bytes and semantic revision.</returns>
    /// <exception cref="EsmGoldenVectorNotFound">No full golden vector is embedded for the version.</exception>
    /// <exception cref="InvalidSemanticContract">The embedded vector is missing or violates the ESM contract.</exception>
    public static EsmGoldenVector Get(int version)
    {
        if (!Versions.Contains(version))
        {
            throw new EsmGoldenVectorNotFound(version);
        }

        return Read(version, $"full-esm-v{version.ToString(CultureInfo.InvariantCulture)}.json");
    }

    static EsmGoldenVector Read(int version, string name)
    {
        using var stream = typeof(EsmGoldenVectors).Assembly.GetManifestResourceStream($"{Prefix}{name}") ??
            throw new InvalidSemanticContract($"ESM golden vector resource '{name}' is missing.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray().ToImmutableArray();
        var model = SemanticModelSerializer.Deserialize(bytes.AsSpan());

        return new(version, bytes, model.Revision);
    }
}

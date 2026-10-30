// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides separately packaged, immutable unknown-preserving reader conformance cases.
/// Cases are not entries in <see cref="EsmGoldenVectors.Versions"/> or real ESM versions.
/// </summary>
public static class EsmConformanceVectors
{
    /// <summary>
    /// Reserves uint.MaxValue for conformance-only pairs under decision 0118's exception
    /// to 0066. These pairs are never allocated, registered or version-table rows, and the
    /// strict reader refuses them. Their fixture bytes freeze at first publication;
    /// new cases use new files. 0066 freezes these bytes, not an ESM version contract.
    /// </summary>
    public const uint ReservedMinor = uint.MaxValue;

    /// <summary>Gets the separately named conformance cases, not version numbers.</summary>
    public static ImmutableArray<string> Names { get; } = ["opaque-v34", "misordered-opaque-v34", "unknown-discriminator-v34"];

    /// <summary>Reads a frozen conformance fixture without interpreting its opaque contents.</summary>
    /// <param name="name">One of <see cref="Names"/>.</param>
    /// <returns>The immutable bytes and expected opt-in admission outcome.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The fixture name is not published.</exception>
    public static EsmConformanceVector Get(string name)
    {
        if (!Names.Contains(name, StringComparer.Ordinal)) throw new ArgumentOutOfRangeException(nameof(name), name, "No ESM conformance vector is published with that name.");
        using var stream = typeof(EsmConformanceVectors).Assembly.GetManifestResourceStream($"Cratis.Screenplay.CanonicalCorpus.EsmConformance.{name}.json")
            ?? throw new InvalidOperationException($"Missing embedded ESM conformance vector '{name}'.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return new(name, [.. buffer.ToArray()], name == "opaque-v34");
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides the hash-pinned protected command read race vectors and the source-backed ESM v25 model they run against.
/// </summary>
/// <remarks>
/// Each vector gives the decided facts, a command request, the expected decision dependencies, and one or more commits.
/// A commit extends the decided world with later facts, or replaces its history, and states the expected outcome. A host runs a
/// vector with <c>EstablishWorld</c>, <c>Decide</c>, <c>ExtendWorld</c> and <c>Commit</c> only, committing the same decision once
/// per commit entry. The conflict contract is target neutral: the boundary is the count of facts in the decided world, a dependency
/// is a key value plus a set of event contracts, and a conflict is a fact after the boundary whose event contract is in the set and
/// whose destination equals the key, compared ordinally. The window between settlement and the guarded write is a host obligation
/// and is not covered by these vectors.
/// </remarks>
public static class ProtectedCommandReadRaceVectors
{
    /// <summary>
    /// The race vector fixture contract version, independent of the ESM version.
    /// </summary>
    public const int Version = 1;

    /// <summary>
    /// The lowercase SHA-256 of the exact UTF-8 fixture bytes, including the trailing newline.
    /// </summary>
    public const string Sha256 = "c964b1529ea7fb11659926aea3acbdb055d969aa5737d4c7c07fb93d193c59f7";

    const string ApplicationName = "ProtectedCommandReadRaces";
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.ProtectedCommandReads.races_v1";

    /// <summary>
    /// Gets the exact immutable bytes of <c>races.v1.json</c> without reserialization.
    /// </summary>
    public static ImmutableArray<byte> Bytes { get; } = Resource("races.v1.json");

    /// <summary>
    /// Gets the source-backed ESM v25 model the vectors run against.
    /// </summary>
    public static CanonicalCorpusVector Model => new()
    {
        Name = "protected-command-reads/races-v1",
        ApplicationName = ApplicationName,
        ApplicationIdentity = ApplicationIdentity.Create(ApplicationName),
        RuntimeStreamId = "reference",
        SourceForms =
        [
            new()
            {
                Name = "single",
                Documents = [new() { StableKey = "races", DisplayPath = "races.play", Bytes = Resource("races.play") }],
                IdentityCatalogBytes = Resource("identity-catalog.json")
            }
        ],
        EsmBytes = Resource("esm-v25.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(ProtectedCommandReadRaceVectors).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Protected command read race resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }
}

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
/// whose destination equals the key, compared ordinally. A constraint rejection from settlement takes precedence over a conflict.
/// The additive V1R2 fixture pins that precedence while preserving the original fixture. The window between settlement and the guarded write is a host obligation
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

    /// <summary>
    /// The fixture contract version used by the additive V1R2 revision; its shape is unchanged from V1.
    /// </summary>
    public const int VersionV1R2 = 1;

    /// <summary>
    /// The lowercase SHA-256 of the exact UTF-8 V1R2 fixture bytes, including the trailing newline.
    /// </summary>
    public const string Sha256V1R2 = "8912d53a4772c39b1396ba3407e242c4c14e692f24cfa02e0de463565d774728";

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

    /// <summary>
    /// Gets the additive fixture revision with every original vector and the coinciding protected-read race and constraint refusal.
    /// </summary>
    public static ImmutableArray<byte> BytesV1R2 => Resource("races.v1r2.json", "Cratis.Screenplay.CanonicalCorpus.Corpus.ProtectedCommandReads.races_v1r2");

    /// <summary>
    /// Gets the unchanged source-backed ESM v25 model under the additive fixture revision's name.
    /// </summary>
    public static CanonicalCorpusVector ModelV1R2 => Model with { Name = "protected-command-reads/races-v1r2" };

    static ImmutableArray<byte> Resource(string name, string prefix = Prefix)
    {
        using var stream = typeof(ProtectedCommandReadRaceVectors).Assembly.GetManifestResourceStream($"{prefix}.{name}") ??
            throw new InvalidSemanticContract($"Protected command read race resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }
}

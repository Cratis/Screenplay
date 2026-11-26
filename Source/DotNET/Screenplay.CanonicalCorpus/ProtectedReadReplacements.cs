// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides runnable keyless replacements and exact dispositions for the frozen protected-read vectors (decision 0171).
/// Originals retain their published bytes, identities, revisions and expectations, as in decision 0168.
/// </summary>
public static class ProtectedReadReplacements
{
    /// <summary>
    /// Gets the SHA-256 of the frozen V25 ESM bytes, not of its replacement.
    /// </summary>
    public const string OriginalV25Sha256 = "253a6d43120098ec895125be91237a00fe20bf10ca31e8ba2e543553a7f28f36";

    /// <summary>
    /// Gets the SHA-256 of the replacement V25 ESM bytes.
    /// </summary>
    public const string V25R2Sha256 = "0b2056c16f932b53e8dff41c76e208f5613629df157ba3361025bfc47ba5e351";

    /// <summary>
    /// Gets the SHA-256 of the exact V1R3 race fixture bytes, including the trailing newline.
    /// </summary>
    public const string RacesV1R3Sha256 = "fd1c9972411f9b5fe2745e1108ede1911f5071c4e836a9b5481dc9b59943d0fb";

    /// <summary>
    /// Gets the V25R2 source-backed vector; every specification must pass normally.
    /// </summary>
    public static CanonicalCorpusVector V25R2 => ProtectedCommandReadsCorpus.V25 with
    {
        Name = "protected-command-reads/v25-r2",
        SourceForms = [.. ProtectedCommandReadsCorpus.SourceForms.Select(ReplaceSource)],
        SpecificationExpectations = [.. ProtectedCommandReadsCorpus.V25.SpecificationExpectations.Select(ReplaceExpectation)],
        EsmBytes = Resource("v25_r2.expected.esm-v25.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("v25_r2.expected.semantic-revision.txt").AsSpan()).Trim())
    };

    /// <summary>
    /// Gets the seventeen-case runnable replacement for the frozen V1 and V1R2 race fixtures.
    /// The fixture contract version, model, case order and all other expectations remain unchanged.
    /// </summary>
    public static ImmutableArray<byte> RacesV1R3 => Resource("races_v1r3.races.v1r3.json");

    /// <summary>
    /// Gets the unchanged race model under the replacement's distinct vector identity.
    /// </summary>
    public static CanonicalCorpusVector RaceModelV1R3 => ProtectedCommandReadRaceVectors.Model with { Name = "protected-command-reads/races-v1r3" };

    /// <summary>
    /// Gets the exact incompatible original V25 specification expectations; no other failures are permitted.
    /// </summary>
    public static ImmutableArray<ProtectedReadVectorDisposition> SpecificationDispositions =>
        [.. ProtectedCommandReadsCorpus.V25.SpecificationExpectations
            .Where(expectation => expectation.RejectionMessage?.StartsWith("Current '", StringComparison.Ordinal) == true)
            .Select(expectation => new ProtectedReadVectorDisposition(
                "ProtectedCommandReadsCorpus.V25",
                ProtectedCommandReadsCorpus.V25.SemanticRevision,
                OriginalV25Sha256,
                "ProtectedReadReplacements.V25R2",
                expectation.Name,
                expectation.RejectionMessage!,
                "Current does not exist."))];

    /// <summary>
    /// Gets the exact superseded commit in each frozen race fixture.
    /// </summary>
    public static ImmutableArray<ProtectedReadVectorDisposition> RaceDispositions =>
    [
        RaceDisposition("ProtectedCommandReadRaceVectors.Bytes", ProtectedCommandReadRaceVectors.Sha256),
        RaceDisposition("ProtectedCommandReadRaceVectors.BytesV1R2", ProtectedCommandReadRaceVectors.Sha256V1R2)
    ];

    static ProtectedReadVectorDisposition RaceDisposition(string vectorId, string sha256) => new(
        vectorId,
        ProtectedCommandReadRaceVectors.Model.SemanticRevision,
        sha256,
        "ProtectedReadReplacements.RacesV1R3",
        "absent-then-created",
        "View 'read' does not exist.",
        "View does not exist.",
        0);

    static CanonicalCorpusSpecificationExpectation ReplaceExpectation(CanonicalCorpusSpecificationExpectation expectation) =>
        expectation.RejectionMessage?.StartsWith("Current '", StringComparison.Ordinal) == true
            ? expectation with { RejectionMessage = "Current does not exist." }
            : expectation;

    static CanonicalCorpusSourceForm ReplaceSource(CanonicalCorpusSourceForm form) => form with
    {
        Documents = [.. form.Documents.Select(document => document with
        {
            Bytes = document.StableKey switch
            {
                "declarations" => Resource("v25_r2.source.declarations.play"),
                "module" => Resource("v25_r2.source.module.play"),
                "application" => [.. Resource("v25_r2.source.declarations.play"), .. Resource("v25_r2.source.module.play")],
                _ => throw new InvalidSemanticContract($"Unknown protected-read source document '{document.StableKey}'.")
            }
        })]
    };

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(ProtectedReadReplacements).Assembly.GetManifestResourceStream($"Cratis.Screenplay.CanonicalCorpus.Corpus.ProtectedCommandReads.{name}") ??
            throw new InvalidSemanticContract($"Protected-read replacement resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }
}

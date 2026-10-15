// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed atomic reaction batch and legacy-prefix conformance vectors.
/// </summary>
public static class AtomicReactionBatchesCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.AtomicReactionBatches.v22";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated sources declaring atomic batches.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms => Forms(true);

    /// <summary>
    /// Gets identical legacy sources with only the explicit mode removed.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> LegacySourceForms => Forms(false);

    /// <summary>
    /// Gets the ESM v22 atomic reaction batch vector.
    /// </summary>
    public static CanonicalCorpusVector V22 => Vector(true);

    /// <summary>
    /// Gets the unopted-in legacy control vector.
    /// </summary>
    public static CanonicalCorpusVector Legacy => Vector(false);

    static ImmutableArray<CanonicalCorpusSourceForm> Forms(bool atomic)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("AtomicReactionBatches"));
        var bytes = SemanticIdentityCatalogSerializer.Serialize(catalog).ToImmutableArray();
        var source = Encoding.UTF8.GetString(Resource("source.declarations.play").AsSpan());
        if (!atomic) source = source.Replace("reaction batches atomic\n", string.Empty, StringComparison.Ordinal);
        var declarations = Document("declarations", "declarations.play", [.. Encoding.UTF8.GetBytes(source)]);
        var module = Document("module", "Accounts/module.play", Resource("source.module.play"));
        var folder = new CanonicalCorpusSourceForm { Name = "folder", Documents = [declarations, module], IdentityCatalogBytes = bytes };
        return
        [
            new() { Name = "single", Documents = [Document("application", "application.play", [.. declarations.Bytes, .. module.Bytes])], IdentityCatalogBytes = bytes },
            folder,
            folder with { Name = "reordered", Documents = [module, declarations] },
            folder with { Name = "relocated", Documents = [module with { DisplayPath = "Archive/module.play" }, declarations with { DisplayPath = "Archive/declarations.play" }] }
        ];
    }

    static CanonicalCorpusVector Vector(bool atomic) => new()
    {
        Name = atomic ? "atomic-reaction-batches/v22" : "atomic-reaction-batches/legacy",
        ApplicationName = "AtomicReactionBatches",
        ApplicationIdentity = ApplicationIdentity.Create("AtomicReactionBatches"),
        RuntimeStreamId = "a",
        SourceForms = atomic ? SourceForms : LegacySourceForms,
        SpecificationExpectations = Expectations(atomic),
        EsmBytes = Resource(atomic ? "expected.esm-v22.json" : "expected.esm-legacy.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource(atomic ? "expected.semantic-revision.txt" : "expected.legacy-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations(bool atomic)
    {
        var application = ApplicationIdentity.Create("AtomicReactionBatches");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var slice = SemanticAddress.ForSlice(application, "Accounts", "Work", "Batches");
        (string Name, string? Error, int Count)[] cases =
        [
            ("LaterEventRejects", "Already claimed", atomic ? 2 : 3),
            ("TwoSourcesCollide", "Already held", atomic ? 1 : 2),
            ("SameSourceRepeats", null, 3),
            ("ReservationSurvivesChange", atomic ? "Already held" : null, atomic ? 1 : 4),
            ("EventCycleReleases", null, 4),
            ("RefusalBatchRejects", "Already claimed", atomic ? 3 : 4),
            ("AcknowledgementHasNoEvents", null, 1),
            ("ApplicationOccurrenceIsOneBatch", null, 3),
            ("ClockOccurrenceIsOneBatch", null, 2)
        ];
        return [.. cases.Select(value => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, value.Name)),
            Name = value.Name,
            Outcome = value.Error is null ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected,
            RejectionMessage = value.Error,
            RejectionCategory = value.Error is null ? null : SemanticRejectionCategory.Constraint,
            WorldFactCount = value.Count,
            Passed = atomic || value.Name != "ReservationSurvivesChange"
        })];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(AtomicReactionBatchesCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Atomic reaction batch corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}

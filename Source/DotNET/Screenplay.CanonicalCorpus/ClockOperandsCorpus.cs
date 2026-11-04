// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed clock operand, command calendar and declared context time zone vectors (decisions 0106, 0107 and 0132).
/// </summary>
public static class ClockOperandsCorpus
{
    const string ApplicationName = "ClockOperands";
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.ClockOperands.v37";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated sources.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalogSerializer.Serialize(SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(ApplicationName))).ToImmutableArray();
            var declarations = Document("declarations", "declarations.play", Resource("source.declarations.play"));
            var module = Document("module", "Scheduling/module.play", Resource("source.module.play"));
            var folder = new CanonicalCorpusSourceForm { Name = "folder", Documents = [declarations, module], IdentityCatalogBytes = catalog };

            return
            [
                new() { Name = "single", Documents = [Document("application", "application.play", [.. declarations.Bytes, .. module.Bytes])], IdentityCatalogBytes = catalog },
                folder,
                folder with { Name = "reordered", Documents = [module, declarations] },
                folder with { Name = "relocated", Documents = [module with { DisplayPath = "Archive/module.play" }, declarations with { DisplayPath = "Archive/declarations.play" }] }
            ];
        }
    }

    /// <summary>
    /// Gets the ESM v37 clock operands vector.
    /// </summary>
    public static CanonicalCorpusVector V37 => new()
    {
        Name = "clock-operands/v37",
        ApplicationName = ApplicationName,
        ApplicationIdentity = ApplicationIdentity.Create(ApplicationName),
        RuntimeStreamId = "due",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v37.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create(ApplicationName);
        var catalog = SemanticIdentityCatalog.Empty(application);
        (string Slice, string Name, string? Error, int Count)[] cases =
        [
            ("Due", "RecentDue", null, 1),
            ("Due", "StaleDue", "The due date is too old.", 0),
            ("Billing", "SameBillingDay", null, 1),
            ("Billing", "EarlierBillingDay", null, 0),
            ("Window", "FreshArrival", null, 1),
            ("Window", "StaleArrival", "The arrival is too old.", 0)
        ];

        return [.. cases.Select(value => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(SemanticAddress.ForSlice(application, "Scheduling", "Clocks", value.Slice), value.Name)),
            Name = value.Name,
            Outcome = value.Error is null ? SemanticExecutionOutcomeKind.Accepted : SemanticExecutionOutcomeKind.Rejected,
            RejectionMessage = value.Error,
            RejectionCategory = value.Error is null ? null : SemanticRejectionCategory.Validation,
            WorldFactCount = value.Count
        })];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(ClockOperandsCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Clock operands corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}

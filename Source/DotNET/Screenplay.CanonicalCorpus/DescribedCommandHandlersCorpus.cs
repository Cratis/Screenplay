// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed described command handler and declarative control vectors.
/// </summary>
public static class DescribedCommandHandlersCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.DescribedCommandHandlers.v23";
    const string Handler = "        handler\n          implementation\n            hint \"Calculate the fee from overdue days\"\n            hint \"Use the agreed daily rate\"\n";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated described command sources.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms => Forms(true);

    /// <summary>
    /// Gets declarative controls without a handler, with a fixed payload mapping instead.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> LegacySourceForms => Forms(false);

    /// <summary>
    /// Gets the ESM v23 described command handler vector.
    /// </summary>
    public static CanonicalCorpusVector V23 => Vector(true);

    /// <summary>
    /// Gets the earlier-version declarative control vector.
    /// </summary>
    public static CanonicalCorpusVector Legacy => Vector(false);

    static ImmutableArray<CanonicalCorpusSourceForm> Forms(bool described)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("DescribedCommandHandlers"));
        var bytes = SemanticIdentityCatalogSerializer.Serialize(catalog).ToImmutableArray();
        var declarations = Document("declarations", "declarations.play", Resource("source.declarations.play"));
        var source = Encoding.UTF8.GetString(Resource("source.module.play").AsSpan());
        if (!described) source = source.Replace(Handler, string.Empty, StringComparison.Ordinal).Replace("          for invoiceId\n", "          for invoiceId\n          amount = 30\n", StringComparison.Ordinal);
        var module = Document("module", "Billing/module.play", [.. Encoding.UTF8.GetBytes(source)]);
        var folder = new CanonicalCorpusSourceForm { Name = "folder", Documents = [declarations, module], IdentityCatalogBytes = bytes };
        return
        [
            new() { Name = "single", Documents = [Document("application", "application.play", [.. declarations.Bytes, .. module.Bytes])], IdentityCatalogBytes = bytes },
            folder,
            folder with { Name = "reordered", Documents = [module, declarations] },
            folder with { Name = "relocated", Documents = [module with { DisplayPath = "Archive/module.play" }, declarations with { DisplayPath = "Archive/declarations.play" }] }
        ];
    }

    static CanonicalCorpusVector Vector(bool described) => new()
    {
        Name = described ? "described-command-handlers/v23" : "described-command-handlers/legacy",
        ApplicationName = "DescribedCommandHandlers",
        ApplicationIdentity = ApplicationIdentity.Create("DescribedCommandHandlers"),
        RuntimeStreamId = "invoice-1",
        SourceForms = described ? SourceForms : LegacySourceForms,
        SpecificationExpectations = Expectations(described),
        EsmBytes = Resource(described ? "expected.esm-v23.json" : "expected.esm-legacy.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource(described ? "expected.semantic-revision.txt" : "expected.legacy-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations(bool described)
    {
        var application = ApplicationIdentity.Create("DescribedCommandHandlers");
        var catalog = SemanticIdentityCatalog.Empty(application);
        (string Slice, string Name, SemanticExecutionOutcomeKind Outcome, int Count)[] cases =
        [
            ("ApplyLateFee", "Denied", SemanticExecutionOutcomeKind.Rejected, 0),
            ("ApplyLateFee", "Invalid", SemanticExecutionOutcomeKind.Rejected, 0),
            ("ApplyLateFee", "Body", described ? SemanticExecutionOutcomeKind.Unsupported : SemanticExecutionOutcomeKind.Accepted, described ? 0 : 1),
            ("ApplyLateFee", "History", SemanticExecutionOutcomeKind.Accepted, 1),
            ("RequestedFees", "InvokedBody", described ? SemanticExecutionOutcomeKind.Unsupported : SemanticExecutionOutcomeKind.Accepted, described ? 1 : 2),
            ("RequestedFees", "InvokedInvalid", SemanticExecutionOutcomeKind.Accepted, 2),
            ("RequestedFees", "InvokedDenied", SemanticExecutionOutcomeKind.Rejected, 1)
        ];
        return [.. cases.Select(value => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(SemanticAddress.ForSlice(application, "Billing", "Fees", value.Slice), value.Name)),
            Name = value.Name,
            Outcome = value.Outcome,
            Passed = value.Outcome != SemanticExecutionOutcomeKind.Unsupported,
            RejectionCategory = Rejection(value.Name),
            UnsupportedCapability = value.Outcome == SemanticExecutionOutcomeKind.Unsupported ? SemanticExecutionCapability.ImplementationBody : null,
            WorldFactCount = value.Count
        })];
    }

    static SemanticRejectionCategory? Rejection(string name) => name switch
    {
        "Denied" or "InvokedDenied" => SemanticRejectionCategory.Unauthorized,
        "Invalid" => SemanticRejectionCategory.Validation,
        _ => null
    };

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(DescribedCommandHandlersCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Described command handler corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);
        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}

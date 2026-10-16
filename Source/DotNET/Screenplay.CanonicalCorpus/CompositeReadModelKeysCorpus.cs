// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>
/// Provides source-backed flat composite read-model key conformance vectors.
/// </summary>
public static class CompositeReadModelKeysCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.CompositeReadModelKeys.v27";

    /// <summary>
    /// Gets equivalent single, folder, reordered and relocated source forms.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> SourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("CompositeReadModelKeys"));
            var catalogBytes = SemanticIdentityCatalogSerializer.Serialize(catalog).ToImmutableArray();
            var declarations = Document("declarations", "declarations.play", Resource("source.declarations.play"));
            var module = Document("module", "Billing/module.play", Resource("source.module.play"));
            var folder = new CanonicalCorpusSourceForm { Name = "folder", Documents = [declarations, module], IdentityCatalogBytes = catalogBytes };
            var reordered = module.Text.Replace(
                "        invoiceId InvoiceId key\n        method PaymentMethod key",
                "        method PaymentMethod key\n        invoiceId InvoiceId key",
                StringComparison.Ordinal).Replace(
                "          invoiceId InvoiceId\n          method PaymentMethod",
                "          method PaymentMethod\n          invoiceId InvoiceId",
                StringComparison.Ordinal);

            return
            [
                new() { Name = "single", Documents = [Document("application", "application.play", [.. declarations.Bytes, .. module.Bytes])], IdentityCatalogBytes = catalogBytes },
                folder,
                folder with { Name = "reordered-documents", Documents = [module, declarations] },
                folder with { Name = "reordered-parts", Documents = [declarations, module with { Bytes = [.. Encoding.UTF8.GetBytes(reordered)] }] },
                folder with { Name = "relocated", Documents = [module with { DisplayPath = "Archive/module.play" }, declarations with { DisplayPath = "Archive/declarations.play" }] }
            ];
        }
    }

    /// <summary>
    /// Gets the flat composite read-model key vector in Double numeric mode.
    /// </summary>
    public static CanonicalCorpusVector V27 => new()
    {
        Name = "composite-read-model-keys/v27",
        ApplicationName = "CompositeReadModelKeys",
        ApplicationIdentity = ApplicationIdentity.Create("CompositeReadModelKeys"),
        RuntimeStreamId = "payment-a",
        SourceForms = SourceForms,
        SpecificationExpectations = Expectations(),
        EsmBytes = Resource("expected.esm-v27.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(Resource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> Expectations()
    {
        var application = ApplicationIdentity.Create("CompositeReadModelKeys");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var slice = SemanticAddress.ForSlice(application, "Billing", "Payments", "Totals");
        string[] names = ["SameInvoiceDifferentMethods", "FilterOnePart", "ScalarListBy", "RemoveOnlyMatchingParts", "RecreateRemovedParts", "InvalidByPart", "InvalidAssertedByPart"];

        return [.. names.Select(name => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, name)),
            Name = name,
            Outcome = name.StartsWith("Invalid", StringComparison.Ordinal) ? SemanticExecutionOutcomeKind.Rejected : SemanticExecutionOutcomeKind.Accepted,
            RejectionCategory = name.StartsWith("Invalid", StringComparison.Ordinal) ? SemanticRejectionCategory.Validation : null,
            RejectionMessage = name.StartsWith("Invalid", StringComparison.Ordinal) ? "Invoice id is too short" : null,
            WorldFactCount = name == "RecreateRemovedParts" ? 3 : name == "RemoveOnlyMatchingParts" ? 1 : 0
        })];
    }

    static ImmutableArray<byte> Resource(string name)
    {
        using var stream = typeof(CompositeReadModelKeysCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{name}") ??
            throw new InvalidSemanticContract($"Composite read-model keys corpus resource '{name}' is missing.");
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return [.. bytes.ToArray()];
    }

    static CanonicalCorpusDocument Document(string key, string path, ImmutableArray<byte> bytes) => new() { StableKey = key, DisplayPath = path, Bytes = bytes };
}

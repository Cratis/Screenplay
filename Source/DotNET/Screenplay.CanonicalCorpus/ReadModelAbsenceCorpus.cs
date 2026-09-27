// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>Provides the source-backed keyed read-model absence conformance vector.</summary>
public static class ReadModelAbsenceCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.ReadModelAbsence.v5";

    /// <summary>Gets the fixed ESM v5 corpus in file and folder forms.</summary>
    public static CanonicalCorpusVector V5 { get; } = Create();

    static CanonicalCorpusVector Create()
    {
        var folder = new CanonicalCorpusSourceForm
        {
            Name = "folder",
            Documents =
            [
                Document("billing-module", "Billing/Billing.play", "source.folder.Billing.Billing.play"),
                Document("invoices-feature", "Billing/Invoices/Invoices.play", "source.folder.Billing.Invoices.Invoices.play"),
                Document("invoice-lookup", "Billing/Invoices/InvoiceLookup/InvoiceLookup.play", "source.folder.Billing.Invoices.InvoiceLookup.InvoiceLookup.play")
            ],
            IdentityCatalogBytes = Resource("identity.folder-catalog-v1.json")
        };
        return new()
        {
            Name = "read-model-absence/v5",
            ApplicationName = "Billing",
            ApplicationIdentity = ApplicationIdentity.Parse("app1:c8a8974a7c463025fcc8cbd2343b89b2dad0c44eab1fd633000cdb181dc3005c"),
            RuntimeStreamId = "first",
            SourceForms =
            [
                new CanonicalCorpusSourceForm
                {
                    Name = "single",
                    Documents = [Document("invoice-removal", "InvoiceRemoval.play", "source.InvoiceRemoval.play")],
                    IdentityCatalogBytes = Resource("identity.single-catalog-v1.json")
                },
                folder,
                new CanonicalCorpusSourceForm
                {
                    Name = "reordered",
                    Documents = [.. folder.Documents.Reverse()],
                    IdentityCatalogBytes = folder.IdentityCatalogBytes
                },
                new CanonicalCorpusSourceForm
                {
                    Name = "relocated",
                    Documents = [.. folder.Documents.Reverse().Select(document => document with { DisplayPath = $"Archive/{document.DisplayPath}" })],
                    IdentityCatalogBytes = folder.IdentityCatalogBytes
                }
            ],
            SpecificationExpectations =
            [
                new CanonicalCorpusSpecificationExpectation
                {
                    Specification = SemanticId.Parse("sem1:aaa31e6d37bd76d6bd2bc5068386235c12cdf2eec1cc87633641ee76bee081a1"),
                    Name = "RemovingOneOfTwoInvoices",
                    Outcome = SemanticExecutionOutcomeKind.Accepted
                }
            ],
            EsmBytes = Resource("expected.esm-v5.json"),
            SemanticRevision = SemanticRevision.Parse("rev1:f9497caeba58c9bc0aecb5f4867d31c8eefc5a197172e0a168282717d221956d")
        };
    }

    static CanonicalCorpusDocument Document(string stableKey, string path, string resource) => new()
    {
        StableKey = stableKey,
        DisplayPath = path,
        Bytes = Resource(resource)
    };

    static ImmutableArray<byte> Resource(string resource)
    {
        using var stream = typeof(ReadModelAbsenceCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{resource}") ??
            throw new InvalidOperationException($"Read-model absence corpus resource '{resource}' is missing.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return [.. memory.ToArray()];
    }
}

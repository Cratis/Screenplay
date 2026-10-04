// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>Provides the source-backed reactions, clock, application trigger and capture conformance vector.</summary>
public static class ReactionsCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.Reactions.v6";

    /// <summary>Gets the fixed ESM v6 corpus in file and folder forms.</summary>
    public static CanonicalCorpusVector V6 { get; } = Create();

    static CanonicalCorpusVector Create()
    {
        var folder = new CanonicalCorpusSourceForm
        {
            Name = "folder",
            Documents =
            [
                Document("collections-module", "Collections/Collections.play", "source.folder.Collections.Collections.play"),
                Document("invoices-feature", "Collections/Invoices/Invoices.play", "source.folder.Collections.Invoices.Invoices.play"),
                Document("send-invoice", "Collections/Invoices/SendInvoice/SendInvoice.play", "source.folder.Collections.Invoices.SendInvoice.SendInvoice.play"),
                Document("close-invoice", "Collections/Invoices/CloseInvoice/CloseInvoice.play", "source.folder.Collections.Invoices.CloseInvoice.CloseInvoice.play"),
                Document("reminders", "Collections/Invoices/Reminders/Reminders.play", "source.folder.Collections.Invoices.Reminders.Reminders.play"),
                Document("legacy-sync", "Collections/Invoices/LegacySync/LegacySync.play", "source.folder.Collections.Invoices.LegacySync.LegacySync.play")
            ],
            IdentityCatalogBytes = Resource("identity.folder-catalog-v1.json")
        };
        return new()
        {
            Name = "reactions/v6",
            ApplicationName = "Collections",
            ApplicationIdentity = ApplicationIdentity.Parse("app1:c8dbc9b9d3686f5177a385e163229df33b9ca6cf55f31b0fd23cd8ed9e3c449e"),
            RuntimeStreamId = "first",
            SourceForms =
            [
                new CanonicalCorpusSourceForm
                {
                    Name = "single",
                    Documents = [Document("collections", "Collections.play", "source.Collections.play")],
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
                Expectation("sem1:3a829ffd770e18dc85cc1ecca5c121cf437ad54d97113a1edc6983dccc6903a9", "SendingAnInvoice"),
                Expectation("sem1:182fc65c0262f892fb967a11f1f8b98bafacdafe7d37b47d761db5393a3f54cb", "ClosingAPaidInvoice"),
                Expectation("sem1:95eed25fd308f7b074895d55f3169ce68266e2bc022f18c7365b9bb0c52ae2c0", "IssuingTheWeeklyDigest"),
                Expectation("sem1:ef2515ee64c918e848e50ec9c9720351c8f1fdae1dc99d887392fda543a2aa24", "SeeingALegacyPayment"),
                Expectation("sem1:16a07b5b5d7fda3deff9fc4f544219b7c60b3fe19f951d79220ea40227a55f1e", "RejectingAnEmptyReason") with
                {
                    Outcome = SemanticExecutionOutcomeKind.Rejected,
                    RejectionCategory = SemanticRejectionCategory.Validation,
                    RejectionMessage = "A reason is required",
                    WorldFactCount = 0
                },
                Expectation("sem1:e95bcc6e3b88097ed6b714b00463dde09ebb28a65195fafa29d49558ffe310fe", "RejectingASecondClose") with
                {
                    Outcome = SemanticExecutionOutcomeKind.Rejected,
                    RejectionCategory = SemanticRejectionCategory.Constraint,
                    RejectionMessage = "Constraint 'OnlyClosedOnce' is violated: the event source already has the constrained event.",
                    WorldFactCount = 1
                },
                Expectation("sem1:31e9ac46cb4c89e2befb8c65f331e9815cfee7afba14a1c68ed71f03f2154559", "UnsupportedStartup") with
                {
                    Outcome = SemanticExecutionOutcomeKind.Unsupported,
                    UnsupportedCapability = SemanticExecutionCapability.Reaction,
                    Passed = false,
                    WorldFactCount = 0
                }
            ],
            EsmBytes = Resource("expected.esm-v6.json"),
            SemanticRevision = SemanticRevision.Parse("rev1:1be294990a5cf564bba0cbc9aae5a1db1d2e0b27387e2a164de2149e8a4f0700")
        };
    }

    static CanonicalCorpusSpecificationExpectation Expectation(string specification, string name) => new()
    {
        Specification = SemanticId.Parse(specification),
        Name = name,
        Outcome = SemanticExecutionOutcomeKind.Accepted
    };

    static CanonicalCorpusDocument Document(string stableKey, string path, string resource) => new()
    {
        StableKey = stableKey,
        DisplayPath = path,
        Bytes = Resource(resource)
    };

    static ImmutableArray<byte> Resource(string resource)
    {
        using var stream = typeof(ReactionsCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{resource}") ??
            throw new InvalidOperationException($"Reactions corpus resource '{resource}' is missing.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return [.. memory.ToArray()];
    }
}

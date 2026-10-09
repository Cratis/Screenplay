// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalCorpus;

/// <summary>Provides the source-backed public events conformance vector.</summary>
public static class PublicEventsCorpus
{
    const string Prefix = "Cratis.Screenplay.CanonicalCorpus.Corpus.PublicEvents.v9";

    /// <summary>Gets the fixed ESM v9 corpus: an outbound translation that publishes a public event and an inbound one that consumes a foreign public event.</summary>
    public static CanonicalCorpusVector V9 { get; } = Create();

    static CanonicalCorpusVector Create() => new()
    {
        Name = "public-events/v9",
        ApplicationName = "Contracts",
        ApplicationIdentity = ApplicationIdentity.Parse("app1:ef7a1a77da447b0443ccfdb7dbbfd4afb3c0e5bd756ffb556bbc1d06afd327f9"),
        RuntimeStreamId = "first",
        SourceForms =
        [
            new CanonicalCorpusSourceForm
            {
                Name = "single",
                Documents = [new CanonicalCorpusDocument { StableKey = "public-events", DisplayPath = "PublicEvents.play", Bytes = Resource("source.PublicEvents.play") }],
                IdentityCatalogBytes = Resource("identity.single-catalog-v1.json")
            }
        ],
        SpecificationExpectations =
        [
            new CanonicalCorpusSpecificationExpectation
            {
                Specification = SemanticId.Parse("sem1:43fb56a40dadc05adc86eb340a1241799683f9b3b1bd941493ff94ca875b7c49"),
                Name = "PublishingAPackedOrder",
                Outcome = SemanticExecutionOutcomeKind.Accepted
            }
        ],
        EsmBytes = Resource("expected.esm-v9.json"),
        SemanticRevision = SemanticRevision.Parse("rev1:8ffa4d7ac03bf38338c7aedb16187229c6ab8139ecfdb0039524e801ea956c2f")
    };

    static ImmutableArray<byte> Resource(string resource)
    {
        using var stream = typeof(PublicEventsCorpus).Assembly.GetManifestResourceStream($"{Prefix}.{resource}") ??
            throw new InvalidOperationException($"Public events corpus resource '{resource}' is missing.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return [.. memory.ToArray()];
    }
}

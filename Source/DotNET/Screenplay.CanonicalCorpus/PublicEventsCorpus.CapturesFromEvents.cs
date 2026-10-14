// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalCorpus;

public static partial class PublicEventsCorpus
{
    /// <summary>
    /// Gets the source forms for the capture-from-event ESM v9 vector.
    /// </summary>
    public static ImmutableArray<CanonicalCorpusSourceForm> CapturesFromEventsSourceForms
    {
        get
        {
            var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("CaptureEvents"));
            return
            [
                new()
                {
                    Name = "single",
                    IdentityCatalogBytes = [.. SemanticIdentityCatalogSerializer.Serialize(catalog)],
                    Documents = [new()
                    {
                        StableKey = "captures-from-events",
                        DisplayPath = "CapturesFromEvents.play",
                        Bytes = CaptureResource("source.CapturesFromEvents.play")
                    }]
                }
            ];
        }
    }

    /// <summary>
    /// Gets the separately pinned capture-from-event vector, leaving the original public-event vector unchanged.
    /// </summary>
    public static CanonicalCorpusVector CapturesFromEventsV9 => new()
    {
        Name = "public-events/captures-from-events/v9",
        ApplicationName = "CaptureEvents",
        ApplicationIdentity = ApplicationIdentity.Create("CaptureEvents"),
        RuntimeStreamId = "parcel",
        SourceForms = CapturesFromEventsSourceForms,
        SpecificationExpectations = CaptureExpectations(),
        EsmBytes = CaptureResource("expected.esm-v9.json"),
        SemanticRevision = SemanticRevision.Parse(Encoding.UTF8.GetString(CaptureResource("expected.semantic-revision.txt").AsSpan()).Trim())
    };

    static ImmutableArray<CanonicalCorpusSpecificationExpectation> CaptureExpectations()
    {
        var application = ApplicationIdentity.Create("CaptureEvents");
        var catalog = SemanticIdentityCatalog.Empty(application);
        var slice = SemanticAddress.ForSlice(application, "Shipping", "Tracking", "Receive");
        return [.. new[] { ("FirstArrival", 2), ("GivenTransition", 3), ("IdenticalArrival", 2), ("Overlay", 3) }.Select(item => new CanonicalCorpusSpecificationExpectation
        {
            Specification = catalog.ResolveSemantic(SemanticAddress.ForSpecification(slice, item.Item1)),
            Name = item.Item1,
            Outcome = SemanticExecutionOutcomeKind.Accepted,
            WorldFactCount = item.Item2
        })];
    }

    static ImmutableArray<byte> CaptureResource(string name)
    {
        using var stream = typeof(PublicEventsCorpus).Assembly.GetManifestResourceStream($"Cratis.Screenplay.CanonicalCorpus.Corpus.PublicEvents.CapturesFromEvents.v9.{name}") ??
            throw new InvalidSemanticContract($"Capture-from-event corpus resource '{name}' is missing.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return [.. memory.ToArray()];
    }
}

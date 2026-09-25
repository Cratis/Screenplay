// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_invalid_event_lineage : Specification
{
    [Fact] void should_reject_a_missing_prior_revision()
    {
        var document = V4();
        var @event = Event(document);
        ((JsonArray)@event["priorRevisions"]!).RemoveAt(0);
        Reject(document);
    }

    [Fact] void should_reject_an_incorrect_predecessor()
    {
        var document = V4();
        Event(document)["predecessor"] = 1;
        Reject(document);
    }

    [Fact] void should_reject_out_of_order_revisions()
    {
        var document = V4();
        var priors = (JsonArray)Event(document)["priorRevisions"]!;
        var first = priors[0]!.DeepClone();
        priors[0] = priors[1]!.DeepClone();
        priors[1] = first;
        Reject(document);
    }

    [Fact] void should_reject_lineage_fields_in_legacy_bytes()
    {
        foreach (var bytes in new[] { canonical_serialization_golden_vectors.SemanticModelBytes,
                     canonical_serialization_golden_vectors.SemanticModelV2Bytes,
                     canonical_serialization_golden_vectors.SemanticModelV3Bytes })
        {
            var document = JsonNode.Parse(bytes)!.AsObject();
            var @event = Event(document);
            @event["contractRevision"] = 2;
            @event["predecessor"] = 1;
            var prior = new JsonObject
            {
                ["contractRevision"] = 1,
                ["predecessor"] = null,
                ["properties"] = new JsonArray(),
                ["tags"] = new JsonArray()
            };
            @event["priorRevisions"] = new JsonArray(prior);
            Catch.Exception(() => SemanticModelSerializer.Deserialize(System.Text.Encoding.UTF8.GetBytes(document.ToJsonString())))
                .Message.ShouldContain("Event contract lineage requires ESM v4.");
        }
    }

    static JsonObject V4() => JsonNode.Parse(canonical_serialization_golden_vectors.SemanticModelV4Bytes)!.AsObject();

    static JsonObject Event(JsonObject document) => document["application"]!["modules"]![0]!["features"]![0]!["features"]![0]!["slices"]![0]!["events"]![0]!.AsObject();

    static void Reject(JsonObject document) => Catch.Exception(() => SemanticModelSerializer.Deserialize(System.Text.Encoding.UTF8.GetBytes(document.ToJsonString())))
        .ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif

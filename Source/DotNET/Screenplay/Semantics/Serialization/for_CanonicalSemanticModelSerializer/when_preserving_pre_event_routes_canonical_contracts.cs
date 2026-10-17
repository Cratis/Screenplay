// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_preserving_pre_event_routes_canonical_contracts : Specification
{
    [Fact]
    void should_preserve_all_seven_versions_and_refuse_every_new_member()
    {
        var vectors = new[]
        {
            (canonical_serialization_golden_vectors.SemanticModelBytes, SemanticVersion.V1),
            (canonical_serialization_golden_vectors.SemanticModelV2Bytes, SemanticVersion.V2),
            (canonical_serialization_golden_vectors.SemanticModelV3Bytes, SemanticVersion.V3),
            (canonical_serialization_golden_vectors.SemanticModelV4Bytes, SemanticVersion.V4),
            (canonical_serialization_golden_vectors.SemanticModelV5Bytes, SemanticVersion.V5),
            (canonical_serialization_golden_vectors.SemanticModelV6Bytes, SemanticVersion.V6),
            (canonical_serialization_golden_vectors.SemanticModelV7Bytes, SemanticVersion.V7)
        };
        vectors.Length.ShouldEqual(7);
        foreach (var (bytes, version) in vectors)
        {
            var read = SemanticModelSerializer.Deserialize(bytes);
            read.SemanticVersion.ShouldEqual(version);
            SemanticModelSerializer.Serialize(read).SequenceEqual(bytes).ShouldBeTrue();
            foreach (var inject in new Action<JsonNode>[]
            {
                root => root["application"]["eventSources"] = new JsonArray(),
                root => when_reading_malformed_event_route_members.Slices(root).First()["commands"][0]["route"] = new JsonObject(),
                root => when_reading_malformed_event_route_members.Slices(root).First()["specifications"][0]["givenEvents"][0]["route"] = new JsonObject(),
                root => when_reading_malformed_event_route_members.Slices(root).First()["specifications"][0]["thenEvents"][0]["route"] = new JsonObject(),
                root => when_reading_malformed_event_route_members.Slices(root).First()["specifications"][0]["thenEvents"][0]["unrouted"] = true,
                root => root["application"]["sourceKind"] = "unknown",
                root => when_reading_malformed_event_route_members.Slices(root).First()["commands"][0]["streamId"] = new JsonObject(),
                root => when_reading_malformed_event_route_members.Slices(root).First()["commands"][0]["streamIdParts"] = new JsonArray()
            })
            {
                var root = JsonNode.Parse(bytes);
                inject(root);
                when_reading_malformed_event_route_members.RejectJson(root.ToJsonString());
            }
            var appendRoot = JsonNode.Parse(bytes);
            var scenario = when_reading_malformed_event_route_members.Slices(appendRoot).First()["specifications"][0];
            var fixture = scenario["thenEvents"][0].DeepClone();
            fixture["route"] = new JsonObject();
            scenario.AsObject().Remove("when");
            scenario["whenAppended"] = fixture;
            when_reading_malformed_event_route_members.RejectJson(appendRoot.ToJsonString());
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer.when_serializing_possible_events;

public class when_refusing_invalid_handler_shapes : Specification
{
    [Fact]
    void should_refuse_possible_events_below_v35()
    {
        var root = Model();
        root["schemaVersion"] = 34;
        root["languageVersion"] = "34.0";
        root["semanticVersion"] = "34.0";
        Refuse(root);
    }

    [Fact]
    void should_refuse_possible_events_on_result_contract_v1()
    {
        var root = Model();
        documents.Find(root, "handler")["resultVersion"] = 1;
        Refuse(root);
    }

    [Fact]
    void should_require_the_v2_event_set()
    {
        var root = Model();
        documents.Find(root, "handler").Remove("possibleEvents");
        Refuse(root);
    }

    [Fact]
    void should_refuse_one_of_in_a_legacy_production()
    {
        var root = Model();
        var command = documents.Find(root, "handler").Parent.AsObject();
        var entry = command["handler"]["possibleEvents"][2].DeepClone().AsObject();
        entry["condition"] = null;
        entry["mappings"] = new JsonArray();
        command["produces"] = new JsonArray(entry);
        Refuse(root);
    }

    static JsonObject Model() => JsonNode.Parse(SemanticModelSerializer.Serialize(given.canonical_serialization_golden_vectors.CreateSemanticModelV35())).AsObject();

    static void Refuse(JsonObject root)
    {
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(documents.Seal(root)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    }
}

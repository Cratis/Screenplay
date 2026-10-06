// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_malformed_v7_members : Specification
{
    [Fact]
    void should_reject_malformed_and_unknown_response_members()
    {
        foreach (var change in new Action<JsonNode>[]
        {
            root => Scalar(root)["response"] = null,
            root => Scalar(root)["response"]["unexpected"] = true,
            root => Scalar(root)["response"]["kind"] = "unknown",
            root => Scalar(root)["response"].AsObject().Remove("source"),
            root => Scalar(root)["response"].AsObject().Remove("type"),
            root => Scalar(root)["response"]["source"] = null,
            root => Scalar(root)["response"]["type"] = null,
            root => Scalar(root)["response"]["fields"] = new JsonArray(),
            root => Record(root)["response"]["fields"] = new JsonArray(),
            root => Record(root)["response"]["fields"][0]["name"] = "",
            root => Record(root)["response"]["fields"][1]["name"] = "token",
            root => Record(root)["response"]["fields"][0].AsObject().Remove("type"),
            root => Record(root)["response"]["fields"][0].AsObject().Remove("source"),
            root => Record(root)["response"]["fields"][0]["unexpected"] = true,
            root => Record(root)["response"]["fields"][0]["source"] = Scalar(root)["properties"][0]["id"].DeepClone()
        })
        {
            Reject(change);
        }
    }
    [Fact]
    void should_reject_malformed_returns_and_generation_fixtures()
    {
        foreach (var change in new Action<JsonNode>[]
        {
            root => Scenario(root)["thenReturns"] = null,
            root => Scenario(root)["thenReturns"]["unexpected"] = true,
            root => Scenario(root)["thenReturns"]["kind"] = "unknown",
            root => Scenario(root)["thenReturns"].AsObject().Remove("value"),
            root => Scenario(root)["thenReturns"]["value"] = null,
            root => Scenario(root)["thenReturns"]["fields"] = new JsonArray(),
            root => RecordScenario(root)["thenReturns"]["fields"] = new JsonArray(),
            root => RecordScenario(root)["thenReturns"]["fields"][0].AsObject().Remove("value"),
            root => RecordScenario(root)["thenReturns"]["fields"][0]["value"] = null,
            root => RecordScenario(root)["thenReturns"]["fields"][0]["unexpected"] = true,
            root => RecordScenario(root)["thenReturns"]["fields"][0]["name"] = "unknown",
            root => RecordScenario(root)["thenReturns"]["fields"][1]["name"] = "note",
            root => Scenario(root)["when"]["generatedValues"] = null,
            root => Scenario(root)["when"]["generatedValues"] = new JsonArray(),
            root => Scenario(root)["when"]["generatedValues"][0]["unexpected"] = true,
            root => Scenario(root)["when"]["generatedValues"][0].AsObject().Remove("value"),
            root => Scenario(root)["when"]["generatedValues"][0]["value"] = null,
            root => Scenario(root)["when"]["generatedValues"][0]["targetProperty"] = Record(root)["properties"][0]["id"].DeepClone(),
            root => Scenario(root)["when"]["values"].AsArray().Add(Scenario(root)["when"]["generatedValues"][0].DeepClone()),
            root => Scenario(root)["when"]["eventSource"] = Scenario(root)["thenEvents"][0]["eventSource"].DeepClone()
        })
        {
            Reject(change);
        }
    }
    [Fact]
    void should_reject_generated_false_and_misplaced_v7_members()
    {
        foreach (var change in new Action<JsonNode>[]
        {
            root => Scalar(root)["properties"][0]["generated"] = false,
            root => Scalar(root)["properties"][0]["generated"] = null,
            root => Scalar(root)["properties"][0]["generated"] = "true",
            root => root["numericMode"] = "legacy",
            root => Scalar(root)["generatedValues"] = new JsonArray(),
            root => Scenario(root)["response"] = Scalar(root)["response"].DeepClone(),
            root => Scenario(root)["when"]["thenReturns"] = Scenario(root)["thenReturns"].DeepClone(),
            root => Slice(root)["events"][0]["properties"][0]["generated"] = true,
            root => root["application"]["types"][0]["properties"][0]["generated"] = true,
            root => root["application"]["triggers"][0]["properties"][0]["generated"] = true,
            root => Scalar(root)["properties"][0]["response"] = Scalar(root)["response"].DeepClone()
        })
        {
            Reject(change);
        }
    }
    [Fact]
    void should_refuse_generated_types_and_concept_rules_in_deserialized_models()
    {
        Reject(root => Scalar(root)["properties"][0]["type"]["optional"] = true);
        Reject(root => Scalar(root)["properties"][0]["type"]["collection"] = true);
        Reject(root => Scalar(root)["properties"][0]["type"] = JsonNode.Parse("""{"kind":"primitive","primitive":"uuid","target":null,"collection":false,"optional":false}"""));
        Reject(root => GeneratedConcept(root)["primitive"] = "string");
        foreach (var kind in new[] { "notEmpty", "rulePredicate", "codeValidation" })
        {
            Reject(root =>
            {
                var rule = new JsonObject { ["property"] = null, ["kind"] = kind, ["operand"] = null, ["message"] = null };
                if (kind != "notEmpty")
                {
                    rule["name"] = "Valid";
                    rule["requirementId"] = "generated-rule";
                }
                GeneratedConcept(root)["validations"] = new JsonArray(rule);
            });
        }
        Reject(root => Scalar(root)["validations"] = new JsonArray(new JsonObject
        {
            ["property"] = Scalar(root)["properties"][0]["id"].DeepClone(),
            ["kind"] = "notEmpty",
            ["operand"] = null,
            ["message"] = null
        }));
    }
    [Fact]
    void should_refuse_policy_references_before_generation()
    {
        foreach (var target in new[] { "artifact", "subject" })
        {
            Reject(root =>
            {
                var condition = new JsonObject { ["kind"] = "claim", ["claim"] = "identity", ["targetKind"] = target };
                if (target == "artifact") condition["value"] = "Id";
                root["application"]["policies"].AsArray().Add(new JsonObject { ["name"] = "GeneratedPolicy", ["condition"] = condition });
                Scalar(root)["authorization"] = new JsonObject { ["kind"] = "policy", ["name"] = "GeneratedPolicy" };
                Scenario(root)["givenCaller"] = JsonNode.Parse("""{"authenticated":true,"roles":[],"claims":[]}""");
            });
        }
    }
    [Fact]
    void should_reject_duplicates()
    {
        var json = Encoding.UTF8.GetString(canonical_serialization_golden_vectors.SemanticModelV7Bytes);
        foreach (var (find, replace) in new[]
        {
            ("\"generated\":true", "\"generated\":true,\"generated\":true"),
            ("\"response\":{", "\"response\":{},\"response\":{"),
            ("\"generatedValues\":[", "\"generatedValues\":[],\"generatedValues\":["),
            ("\"thenReturns\":{", "\"thenReturns\":{},\"thenReturns\":{"),
            ("\"kind\":\"scalar\",\"source\":", "\"kind\":\"scalar\",\"kind\":\"scalar\",\"source\":"),
            ("\"name\":\"token\",\"source\":", "\"name\":\"token\",\"name\":\"token\",\"source\":")
        })
        {
            json.Contains(find, StringComparison.Ordinal).ShouldBeTrue();
            RejectJson(json.Replace(find, replace, StringComparison.Ordinal));
        }
    }
    [Fact]
    void should_refuse_each_v7_member_in_every_older_schema()
    {
        var bytes = new[] { canonical_serialization_golden_vectors.SemanticModelBytes, canonical_serialization_golden_vectors.SemanticModelV2Bytes, canonical_serialization_golden_vectors.SemanticModelV3Bytes, canonical_serialization_golden_vectors.SemanticModelV4Bytes, canonical_serialization_golden_vectors.SemanticModelV5Bytes, canonical_serialization_golden_vectors.SemanticModelV6Bytes };
        bytes.Length.ShouldEqual(6);
        foreach (var old in bytes)
        {
            foreach (var inject in new Action<JsonNode>[]
            {
                root => Commands(root).First()["properties"][0]["generated"] = true,
                root => Commands(root).First()["response"] = new JsonObject(),
                root => Specifications(root).First(value => value["when"] is not null)["when"]["generatedValues"] = new JsonArray(),
                root => Specifications(root).First()["thenReturns"] = new JsonObject(),
                root => root["numericMode"] = "legacy"
            })
            {
                var root = JsonNode.Parse(old);
                inject(root);
                RejectJson(root.ToJsonString());
            }
        }
    }

    static void Reject(Action<JsonNode> change)
    {
        var root = JsonNode.Parse(canonical_serialization_golden_vectors.SemanticModelV7Bytes);
        change(root);
        RejectJson(root.ToJsonString());
    }

    static void RejectJson(string json)
    {
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.Contains("revision", StringComparison.Ordinal).ShouldBeFalse();
    }

    static IEnumerable<JsonNode> Slices(JsonNode root) => root["application"]["modules"].AsArray().SelectMany(module => module["features"].AsArray()).OfType<JsonNode>().SelectMany(AllSlices);
    static IEnumerable<JsonNode> AllSlices(JsonNode feature) => feature["slices"].AsArray().OfType<JsonNode>().Concat(feature["features"].AsArray().OfType<JsonNode>().SelectMany(AllSlices));
    static IEnumerable<JsonNode> Commands(JsonNode root) => Slices(root).SelectMany(slice => slice["commands"].AsArray()).OfType<JsonNode>();
    static IEnumerable<JsonNode> Specifications(JsonNode root) => Slices(root).SelectMany(slice => slice["specifications"].AsArray()).OfType<JsonNode>();
    static JsonNode GeneratedConcept(JsonNode root) => root["application"]["concepts"].AsArray().OfType<JsonNode>().Single(concept => concept["name"].GetValue<string>() == "GeneratedIdentity");
    static JsonNode Slice(JsonNode root) => Slices(root).Single(slice => slice["name"].GetValue<string>() == "Responses");
    static JsonNode Scalar(JsonNode root) => Slice(root)["commands"].AsArray().OfType<JsonNode>().Single(command => command["name"].GetValue<string>() == "CreateEntity");
    static JsonNode Record(JsonNode root) => Commands(root).Single(command => command["name"].GetValue<string>() == "CreateToken");
    static JsonNode Scenario(JsonNode root) => Specifications(root).Single(specification => specification["name"].GetValue<string>() == "returns a generated identifier");
    static JsonNode RecordScenario(JsonNode root) => Specifications(root).Single(specification => specification["name"].GetValue<string>() == "returns a record with a null field");
}

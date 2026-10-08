// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_malformed_event_route_members : Specification
{
    internal static JsonNode Root() => JsonNode.Parse(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateEventRoutesModel()))!;
    internal static JsonNode Source(JsonNode root) => root["application"]["eventSources"].AsArray().OfType<JsonNode>().Single(source => source["name"].GetValue<string>() == "RenamedProject");
    internal static JsonNode Slice(JsonNode root) => Slices(root).Single(slice => slice["name"].GetValue<string>() == "EventRoutes");
    internal static JsonNode Command(JsonNode root) => Slice(root)["commands"].AsArray().OfType<JsonNode>().Single(command => command["name"].GetValue<string>() == "Route9100");
    internal static JsonNode Composite(JsonNode root) => Slice(root)["commands"].AsArray().OfType<JsonNode>().Single(command => command["name"].GetValue<string>() == "CompositeRoute");
    internal static JsonNode Scenario(JsonNode root) => Slice(root)["specifications"][0]!;
    internal static IEnumerable<JsonNode> Slices(JsonNode root) => root["application"]["modules"].AsArray().SelectMany(module => module["features"].AsArray()).OfType<JsonNode>().SelectMany(AllSlices);
    static IEnumerable<JsonNode> AllSlices(JsonNode feature) => feature["slices"].AsArray().OfType<JsonNode>().Concat(feature["features"].AsArray().OfType<JsonNode>().SelectMany(AllSlices));
    internal static void RejectJson(string json)
    {
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.Contains("revision", StringComparison.Ordinal).ShouldBeFalse();
    }
    static void Reject(Action<JsonNode> change)
    {
        var root = Root();
        change(root);
        RejectJson(root.ToJsonString());
    }

    [Fact]
    void should_refuse_malformed_source_and_stream_members()
    {
        foreach (var change in new Action<JsonNode>[]
        {
            root => root["application"]["eventSources"] = null,
            root => root["application"]["eventSources"] = new JsonArray(),
            root => Source(root)["identifierType"] = null,
            root => Source(root)["streams"] = null,
            root => Source(root)["sourceKind"] = "Default",
            root => Source(root)["sourceKind"] = "Fallback",
            root => Source(root)["unexpected"] = true,
            root => Source(root)["streams"][0]["streamKind"] = "",
            root => Source(root)["streams"][0]["streamKind"] = Source(root)["streams"][1]["streamKind"].DeepClone(),
            root => Source(root)["streams"][0]["streamIdType"] = null,
            root => Source(root)["streams"][0]["streamIdParts"] = new JsonArray(),
            root => Source(root)["streams"][0]["streamIdParts"] = Source(root)["streams"][4]["streamIdParts"].DeepClone(),
            root => Source(root)["streams"][4]["streamIdParts"].AsArray().RemoveAt(0),
            root => Source(root)["streams"][4]["streamIdParts"][1]["name"] = "projectId",
            root => Source(root)["streams"][4]["streamIdParts"][0]["type"] = null
        })
        {
            Reject(change);
        }
    }

    [Fact]
    void should_refuse_malformed_command_route_members()
    {
        foreach (var change in new Action<JsonNode>[]
        {
            root => Command(root)["route"] = null,
            root => Command(root)["route"] = new JsonObject(),
            root => Command(root)["route"]["unexpected"] = true,
            root => Command(root)["route"]["source"] = null,
            root => Command(root)["route"]["stream"] = root["application"]["eventSources"][1]["streams"][0]["id"].DeepClone(),
            root => Command(root)["route"]["streamId"] = null,
            root => Command(root)["route"].AsObject().Remove("streamId"),
            root => Command(root)["route"]["streamIdParts"] = new JsonArray(),
            root => Command(root)["route"]["streamIdParts"] = Composite(root)["route"]["streamIdParts"].DeepClone(),
            root => Composite(root)["route"]["streamId"] = Command(root)["route"]["streamId"].DeepClone(),
            root => Composite(root)["route"]["streamIdParts"][0]["value"] = null,
            root => Composite(root)["route"]["streamIdParts"][0]["part"] = "extra",
            root => Composite(root)["route"]["streamIdParts"][1]["part"] = "projectId",
            root => Composite(root)["route"]["streamIdParts"].AsArray().RemoveAt(0),
            root => Composite(root)["route"]["streamIdParts"].AsArray().Add(Composite(root)["route"]["streamIdParts"][0].DeepClone()),
            root => Command(root)["route"]["stream"] = Source(root)["streams"][3]["id"].DeepClone()
        })
        {
            Reject(change);
        }
    }

    [Fact]
    void should_refuse_malformed_fixture_and_unrouted_members()
    {
        foreach (var change in new Action<JsonNode>[]
        {
            root => Scenario(root)["givenEvents"][0]["route"] = null,
            root => Scenario(root)["givenEvents"][0]["route"] = new JsonObject(),
            root => Scenario(root)["givenEvents"][0]["route"]["streamId"] = null,
            root => Scenario(root)["givenEvents"][0]["route"]["streamId"]["value"] = "not a uuid",
            root => Scenario(root)["givenEvents"][0]["route"]["unexpected"] = true,
            root => Scenario(root)["givenEvents"][0]["unrouted"] = true,
            root => Scenario(root)["givenEvents"][0].AsObject().Remove("eventSource"),
            root => Scenario(root)["thenEvents"][0]["unrouted"] = true,
            root => Scenario(root)["thenEvents"][0]["unrouted"] = false,
            root => Scenario(root)["thenEvents"][0]["unrouted"] = null,
            root => Scenario(root)["thenEvents"][0]["unrouted"] = "true",
            root => Slice(root)["specifications"].AsArray().Single(spec => spec["name"].GetValue<string>() == "Append routed history without a producer")["whenAppended"]["unrouted"] = true,
            root => Scenario(root)["when"]["route"] = Scenario(root)["givenEvents"][0]["route"].DeepClone(),
            root => Scenario(root)["route"] = Scenario(root)["givenEvents"][0]["route"].DeepClone()
        })
        {
            Reject(change);
        }
    }

    [Fact]
    void should_refuse_duplicate_members()
    {
        var json = Root().ToJsonString();
        foreach (var member in new[] { "eventSources", "sourceKind", "streamKind", "streamIdType", "streamIdParts", "route", "source", "stream", "streamId", "part", "unrouted" })
        {
            var needle = $"\"{member}\":";
            json.Contains(needle, StringComparison.Ordinal).ShouldBeTrue();
            RejectJson(json.Replace(needle, $"{needle}null,{needle}", StringComparison.Ordinal));
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_CompositeReadModelKeysCorpus;

public class when_reading_unadmitted_projection_parts : given.a_flat_key_model
{
    [Theory]
    [InlineData("from", "non-key")]
    [InlineData("removedWith", "non-key")]
    [InlineData("from", "type")]
    [InlineData("removedWith", "type")]
    [InlineData("from", "scalar")]
    [InlineData("removedWith", "scalar")]
    [InlineData("from", "parent")]
    [InlineData("removedWith", "parent")]
    public void should_reject_invalid_keys_before_revision_comparison(string block, string mutation)
    {
        var node = Model();
        var slice = Slice(node);
        var entry = slice["projections"]![0]!["scope"]![block]![0]!;
        var parts = (JsonArray)entry["key"]!["parts"]!;
        switch (mutation)
        {
            case "non-key": parts[0]!["property"] = ((JsonArray)slice["readModels"]![0]!["properties"]!).Single(property => property!["name"]!.GetValue<string>() == "quantity")!["id"]!.DeepClone(); break;
            case "type": parts[0]!["value"] = JsonNode.Parse("""{"kind":"literal","value":{"kind":"boolean","value":true}}"""); break;
            case "scalar": entry["key"] = JsonNode.Parse("""{"kind":"value","value":{"kind":"eventSourceIdentity"}}"""); break;
            case "parent": entry["parentKey"] = entry["key"]!.DeepClone(); break;
        }
        Refuses(node);
    }

    [Theory]
    [InlineData("join")]
    [InlineData("children")]
    [InlineData("nested")]
    [InlineData("every")]
    [InlineData("all")]
    [InlineData("removedWithJoin")]
    public void should_reject_unadmitted_scopes_before_revision_comparison(string block)
    {
        var node = Model();
        var slice = Slice(node);
        var scope = slice["projections"]![0]!["scope"]!;
        var property = slice["readModels"]![0]!["properties"]![0]!["id"]!.GetValue<string>();
        switch (block)
        {
            case "join":
                var join = scope["from"]![0]!.DeepClone();
                join.AsObject().Remove("parentKey");
                join["on"] = property;
                scope["join"]!.AsArray().Add(join);
                break;
            case "children":
            case "nested":
                var child = new JsonObject { ["property"] = property, ["scope"] = scope.DeepClone() };
                if (block == "children") child["identifiedBy"] = property;
                scope[block]!.AsArray().Add(child);
                break;
            case "every":
            case "all":
                scope["every"] = new JsonObject { ["includeChildren"] = false, ["subscribesToAllEvents"] = block == "all", ["mappings"] = new JsonArray() };
                break;
            case "removedWithJoin":
                var removal = scope["removedWith"]![0]!.DeepClone();
                removal.AsObject().Remove("parentKey");
                scope["removedWithJoin"]!.AsArray().Add(removal);
                break;
        }
        Refuses(node, "root from and remove with");
    }

    [Theory]
    [InlineData(1, "keyParts")]
    [InlineData(25, "keyParts")]
    [InlineData(26, "keyParts")]
    [InlineData(1, "parts")]
    [InlineData(25, "parts")]
    public void should_reject_new_members_even_without_several_identifiers(int version, string member)
    {
        if (member == "parts")
        {
            using var resource = typeof(CompositeReadModelKeysCorpus).Assembly.GetManifestResourceStream($"Cratis.Screenplay.CanonicalCorpus.Esm.full-esm-v{version}.json")!;
            using var buffer = new MemoryStream();
            resource.CopyTo(buffer);
            var bytes = buffer.ToArray();
            SemanticModelSerializer.Deserialize(bytes).SemanticVersion.ShouldEqual(SemanticVersion.Parse($"{version}.0"));
            var legacy = JsonNode.Parse(bytes)!;
            var projection = Descendants(legacy).OfType<JsonObject>().First(value => value["scope"] is JsonObject scope && scope["from"] is JsonArray { Count: > 0 });
            projection["scope"]!["from"]![0]!["key"] = Slice(Model())["projections"]![0]!["scope"]!["from"]![0]!["key"]!.DeepClone();
            Refuses(legacy, "projection key variant");
            return;
        }
        var node = Model();
        var slice = Slice(node);
        var keys = ((JsonArray)slice["readModels"]![0]!["properties"]!).Where(property => property!["identifier"]!.GetValue<bool>()).ToArray();
        keys[1]!["identifier"] = false;
        slice["projections"] = new JsonArray();
        node["schemaVersion"] = version;
        node["languageVersion"] = $"{version}.0";
        node["semanticVersion"] = $"{version}.0";
        Refuses(node, member);
    }

    [Fact]
    void should_reject_a_programmatic_reducer_over_flat_keys()
    {
        var node = Model();
        var slice = Slice(node);
        slice["reducers"] = new JsonArray(new JsonObject
        {
            ["name"] = "Reduce",
            ["readModel"] = slice["readModels"]![0]!["id"]!.GetValue<string>(),
            ["key"] = "eventSourceId",
            ["initialState"] = null,
            ["result"] = "stateOrDelete",
            ["transitions"] = new JsonArray()
        });
        Refuses(node, "reducer over a composite-key read model");
    }

    static JsonNode Model()
    {
        var compiled = Compile(Declarations + "\n" + """
                  projection Rows => Row
                    from Recorded
                      key
                        first = first
                        second = second
                    remove with Removed
                      key
                        first = "a"
                        second = "b"
            """);
        Assert.True(compiled.Success, string.Join('\n', compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));

        return JsonNode.Parse(SemanticModelSerializer.Serialize(compiled.Value!.Model))!;
    }

    static IEnumerable<JsonNode> Descendants(JsonNode node)
    {
        yield return node;
        var children = node switch
        {
            JsonObject obj => obj.Select(pair => pair.Value),
            JsonArray array => array.AsEnumerable(),
            _ => []
        };
        foreach (var child in children.OfType<JsonNode>())
        {
            foreach (var descendant in Descendants(child)) yield return descendant;
        }
    }

    static JsonNode Slice(JsonNode node) => node["application"]!["modules"]![0]!["features"]![0]!["slices"]![0]!;

    static void Refuses(JsonNode node, string? message = null)
    {
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(node.ToJsonString())));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        Assert.DoesNotContain("revision", error.Message, StringComparison.OrdinalIgnoreCase);
        if (message is not null) Assert.Contains(message, error.Message, StringComparison.OrdinalIgnoreCase);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader.given;

public static class documents
{
    public static EsmAdmissionRange Range(uint major = 1) => new(major, 0, uint.MaxValue);

    public static byte[] Frozen(uint major)
    {
        using var stream = typeof(documents).Assembly.GetManifestResourceStream($"Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v{major}.json")!;
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    public static JsonObject Future(uint major = 1)
    {
        var root = JsonNode.Parse(Frozen(major))!.AsObject();
        root["languageVersion"] = $"{major}.{uint.MaxValue}";
        root["semanticVersion"] = $"{major}.{uint.MaxValue}";
        return root;
    }

    public static JsonObject Minimal()
    {
        return new JsonObject
        {
            ["schema"] = SemanticModelCanonicalJson.Schema,
            ["schemaVersion"] = 1,
            ["languageVersion"] = $"1.{uint.MaxValue}",
            ["semanticVersion"] = $"1.{uint.MaxValue}",
            ["revision"] = $"rev1:{new string('0', 64)}",
            ["application"] = new JsonObject
            {
                ["id"] = Id(1), ["name"] = "Future", ["concepts"] = new JsonArray(), ["types"] = new JsonArray(), ["modules"] = new JsonArray()
            }
        };
    }

    public static string Id(int value) => $"sem1:{value:x64}";

    public static byte[] Seal(JsonObject root)
    {
        var unrevisioned = root.DeepClone().AsObject();
        unrevisioned.Remove("revision");
        root["revision"] = SemanticRevision.Compute(Render(unrevisioned)).ToString();
        return Render(root);
    }

    public static string SealText(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        var revision = parsed.RootElement.GetProperty("revision").GetString()!;
        var withoutRevision = json.Replace($",\"revision\":\"{revision}\"", string.Empty, StringComparison.Ordinal);
        var computed = SemanticRevision.Compute(Encoding.UTF8.GetBytes(withoutRevision));
        return json.Replace(revision, computed.ToString(), StringComparison.Ordinal);
    }

    public static byte[] Render(JsonNode root)
    {
        var buffer = new ArrayBufferWriter<byte>();
        var options = CanonicalJson.WriterOptions;
        options.MaxDepth = 128;
        using var writer = new Utf8JsonWriter(buffer, options);
        Write(writer, root);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    public static JsonObject Find(JsonNode node, string property) => FindOptional(node, property) ?? throw new InvalidOperationException($"Fixture has no object-valued '{property}'.");

    static JsonObject? FindOptional(JsonNode node, string property)
    {
        if (node is JsonObject obj && obj[property] is JsonObject result) return result;
        return Children(node).OfType<JsonNode>().Select(child => FindOptional(child, property)).FirstOrDefault(found => found is not null);
    }

    static IEnumerable<JsonNode?> Children(JsonNode node) => node switch
    {
        JsonObject obj => obj.Select(pair => pair.Value),
        JsonArray array => array,
        _ => []
    };

    static void Write(Utf8JsonWriter writer, JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj: WriteObject(writer, obj); break;
            case JsonArray array:
                writer.WriteStartArray();
                foreach (var item in array) Write(writer, item);
                writer.WriteEndArray();
                break;
            case null: writer.WriteNullValue(); break;
            default:
                using (var primitive = JsonDocument.Parse(node.ToJsonString())) EsmOpaqueJson.Write(writer, primitive.RootElement);
                break;
        }
    }

    static void WriteObject(Utf8JsonWriter writer, JsonObject obj)
    {
        writer.WriteStartObject();
        foreach (var pair in obj)
        {
            // The baseline's schema operator uses Utf8JsonWriter escaping, unlike semantic text.
            if (pair.Key == "operator") writer.WriteString(pair.Key, pair.Value!.GetValue<string>());
            else WriteProperty(writer, pair);
        }

        writer.WriteEndObject();
    }

    static void WriteProperty(Utf8JsonWriter writer, KeyValuePair<string, JsonNode?> pair)
    {
        writer.WritePropertyName(pair.Key);
        Write(writer, pair.Value);
    }
}
#endif

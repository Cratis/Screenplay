// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Comparison;

internal static partial class StructuralComparison
{
    static Dictionary<string, string> Members(IEnumerable<SyntaxNode> nodes) => nodes.SelectMany(node => SyntaxJson.Serialize(node).EnumerateObject()
        .Where(property => !_ignoredMembers.Contains(property.Name, StringComparer.Ordinal) && (!(node is ApplicationSyntax or ModuleSyntax or FeatureSyntax or SliceSyntax) || !_hierarchyChildren.Contains(property.Name, StringComparer.Ordinal)))
        .Select(property => new KeyValuePair<string, string>(node is EventSyntax @event ? $"generation:{@event.Generation}/{property.Name}" : property.Name, Normalize(property.Value))))
        .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    static bool StreamSchemaChanged(IEnumerable<SyntaxNode> before, IEnumerable<SyntaxNode> after)
    {
        var previous = before.OfType<EventStreamSyntax>().ToArray();
        var current = after.OfType<EventStreamSyntax>().ToArray();
        if (previous.Length != 1 || current.Length != 1) return false;
        static string Schema(EventStreamSyntax stream)
        {
            if (stream.StreamId is { } scalar) return $"scalar:{SyntaxJson.Serialize(scalar).GetRawText()}";

            return stream.StreamIdParts.Any()
                ? $"composite:{string.Join('|', stream.StreamIdParts.Select(part => SyntaxJson.Serialize(part.Type).GetRawText()))}"
                : "unkeyed";
        }

        if (Schema(previous[0]) != Schema(current[0])) return true;
        var previousParts = previous[0].StreamIdParts.ToArray();
        var currentParts = current[0].StreamIdParts.ToArray();
        for (var index = 0; index < previousParts.Length; index++)
        {
            var position = Array.FindIndex(currentParts, part => part.Name == previousParts[index].Name);
            if (position >= 0 && position != index) return true;
        }

        return false;
    }

    static string MemberChange(string member, string? before, string? after, bool outcome)
    {
        if (Opaque(before, member) != Opaque(after, member)) return "opaque-changed";
        return outcome ? "expected-outcome-changed" : "changed";
    }

    static string Opaque(string? value, string member)
    {
        if (member == "file" || member.EndsWith("/file", StringComparison.Ordinal)) return value ?? "null";
        if (value is null) return "{}";
        var values = new JsonObject();
        Collect(JsonNode.Parse(value), string.Empty);
        return values.ToJsonString();

        void Collect(JsonNode? node, string path)
        {
            if (node is JsonObject obj)
            {
                foreach (var pair in obj)
                {
                    var key = $"{path}/{pair.Key}";
                    if (_opaqueMembers.Contains(pair.Key, StringComparer.Ordinal)) values[key] = pair.Value?.DeepClone();
                    else Collect(pair.Value, key);
                }
            }
            if (node is JsonArray array)
            {
                for (var index = 0; index < array.Count; index++) Collect(array[index], $"{path}/{index}");
            }
        }
    }

    static string Normalize(JsonElement value)
    {
        var node = JsonNode.Parse(value.GetRawText());
        Strip(node);
        return node?.ToJsonString() ?? "null";
    }

    static void Strip(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            foreach (var key in obj.Select(pair => pair.Key).Where(key => key == "description" || key == "documentation").ToArray()) obj.Remove(key);
            foreach (var child in obj.Select(pair => pair.Value)) Strip(child);
        }
        if (node is JsonArray array)
        {
            foreach (var child in array) Strip(child);
        }
    }
}

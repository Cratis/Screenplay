// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpLineage.given;

public class a_lineage_model : for_McpConnection.given.a_connection
{
    internal void Write(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source, new UTF8Encoding(false));
        Initialize();
    }

    internal JsonElement Trace(string address, int limit = 200) =>
        Call("trace-lineage", new { address, limit }).GetProperty("result").GetProperty("structuredContent");

    internal static JsonElement[] Hops(JsonElement report) => [.. report.GetProperty("page").GetProperty("items").EnumerateArray()];

    internal static JsonElement[] Children(JsonElement[] hops, JsonElement parent) =>
        [.. hops.Where(hop => hop.GetProperty("parent").ValueKind == JsonValueKind.Number && hop.GetProperty("parent").GetInt32() == parent.GetProperty("id").GetInt32())];

    internal static JsonElement Subject(JsonElement[] hops) => hops.Single(hop => hop.GetProperty("direction").GetString() == "subject");

    internal static string? Text(JsonElement hop, string property) => hop.GetProperty(property).ValueKind == JsonValueKind.Null ? null : hop.GetProperty(property).GetString();

    // The chain of roles from the subject down to a leaf, following parent ids back up.
    internal static string[] Chain(JsonElement[] hops, JsonElement leaf, string property = "role")
    {
        var path = new List<string>();
        var current = leaf;
        while (true)
        {
            path.Insert(0, Text(current, property) ?? string.Empty);
            if (current.GetProperty("parent").ValueKind != JsonValueKind.Number) break;
            var parent = current.GetProperty("parent").GetInt32();
            current = hops.Single(hop => hop.GetProperty("id").GetInt32() == parent);
        }

        return [.. path];
    }

    internal static string Root([System.Runtime.CompilerServices.CallerFilePath] string path = "")
    {
        var directory = Directory.GetParent(path);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "Samples"))) directory = directory.Parent;

        return directory!.FullName;
    }
}

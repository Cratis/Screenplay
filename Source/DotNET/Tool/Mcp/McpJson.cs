// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(), new McpSyntaxJson() }
    };

    internal static readonly JsonElement Empty = JsonSerializer.SerializeToElement(new { });

    internal static string RequiredString(JsonElement value, string name) =>
        OptionalString(value, name) ?? throw new McpFailure($"'{name}' is required.", -32602);

    internal static string? OptionalString(JsonElement value, string name)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new McpFailure("Arguments must be an object.", -32602);
        }

        if (!value.TryGetProperty(name, out var property))
        {
            return null;
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            throw new McpFailure($"'{name}' must be a string.", -32602);
        }

        return property.GetString();
    }

    internal static object ToolResult(object value, bool isError = false) => new
    {
        content = new[] { new { type = "text", text = JsonSerializer.Serialize(value, Options) } },
        structuredContent = value,
        isError
    };
}

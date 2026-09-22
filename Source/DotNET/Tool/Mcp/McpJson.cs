// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpJson
{
    internal const int MaximumStructuredResponseBytes = 1024 * 1024;
    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        MaxDepth = 256,
        Converters = { new JsonStringEnumConverter(), new McpSyntaxJson() }
    };

    internal static readonly JsonElement Empty = JsonSerializer.SerializeToElement(new { });
    internal static readonly JsonElement EmptyArray = JsonSerializer.SerializeToElement(Array.Empty<object>());

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

    internal static void ValidateObject(JsonElement value, IReadOnlyCollection<string> allowed, IReadOnlyCollection<string> required)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            throw new McpFailure("Arguments must be an object.", -32602);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (!seen.Add(property.Name) || !allowed.Contains(property.Name, StringComparer.Ordinal))
            {
                throw new McpFailure($"Unexpected or duplicate argument '{property.Name}'.", -32602);
            }
        }

        foreach (var name in required.Where(name => !seen.Contains(name)))
        {
            throw new McpFailure($"'{name}' is required.", -32602);
        }
    }

    internal static int Integer(JsonElement value, string name, int fallback, int minimum, int maximum)
    {
        if (!value.TryGetProperty(name, out var property))
        {
            return fallback;
        }

        if (property.ValueKind != JsonValueKind.Number || !property.TryGetInt32(out var number) || number < minimum || number > maximum)
        {
            throw new McpFailure($"'{name}' must be an integer between {minimum} and {maximum}.", -32602);
        }

        return number;
    }

    internal static bool Boolean(JsonElement value, string name, bool fallback = false)
    {
        if (!value.TryGetProperty(name, out var property))
        {
            return fallback;
        }

        if (property.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new McpFailure($"'{name}' must be a boolean.", -32602);
        }

        return property.GetBoolean();
    }

    internal static T Enumeration<T>(JsonElement value, string name, T fallback)
        where T : struct, Enum
    {
        var text = OptionalString(value, name);
        if (text is null)
        {
            return fallback;
        }

        return Enum.TryParse<T>(text, out var result) && Enum.GetName(result) == text
            ? result
            : throw new McpFailure($"'{name}' must be one of {string.Join(", ", Enum.GetNames<T>())}.", -32602);
    }

    internal static object ToolResult(object value, bool isError = false, bool enforceBudget = true)
    {
        // Materialize once: deferred indexes and AST projections must not execute again for the text copy.
        var structured = JsonSerializer.SerializeToElement(value, Options);
        var text = structured.GetRawText();
        if (enforceBudget && Encoding.UTF8.GetByteCount(text) > MaximumStructuredResponseBytes)
        {
            throw new McpFailure("ResponseTooLarge: use narrower scope, smaller pages, or chunked document/workspace retrieval. No result was truncated.");
        }

        return new
        {
            content = new[] { new { type = "text", text } },
            structuredContent = structured,
            isError
        };
    }
}

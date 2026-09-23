// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp;

static class McpAstSchemas
{
    internal static JsonObject Handle()
    {
        var properties = new JsonObject
        {
            ["revision"] = String(),
            ["documentId"] = String(),
            ["path"] = String()
        };
        return Object(properties, "revision", "documentId", "path");
    }

    internal static JsonObject Node() => new()
    {
        ["type"] = "object",
        ["description"] = "Typed syntax payload. Discover the exact member schema using syntax-schema for its kind. Source locations are server-owned.",
        ["properties"] = new JsonObject { ["kind"] = String() },
        ["required"] = new JsonArray("kind"),
        ["additionalProperties"] = true
    };

    internal static JsonObject Operations() => new()
    {
        ["oneOf"] = new JsonArray(
            Operation("add", new() { ["parent"] = Handle(), ["expectedParent"] = Node(), ["member"] = String(), ["node"] = Node(), ["index"] = Integer() }, "parent", "member", "node"),
            Operation("replace", new() { ["target"] = Handle(), ["expected"] = Node(), ["node"] = Node() }, "target", "node"),
            Operation("remove", new() { ["target"] = Handle(), ["expected"] = Node() }, "target"),
            Operation("move", new() { ["target"] = Handle(), ["expected"] = Node(), ["parent"] = Handle(), ["expectedParent"] = Node(), ["member"] = String(), ["index"] = Integer() }, "target", "parent", "member"))
    };

    internal static JsonObject Documents() => new()
    {
        ["oneOf"] = new JsonArray(
            Operation("create-document", new() { ["stableKey"] = String(), ["path"] = String(), ["node"] = Node(), ["encoding"] = Choice("Utf8", "Utf8WithBom") }, "stableKey", "path", "node"),
            Operation("replace-document", new() { ["documentId"] = String(), ["node"] = Node() }, "documentId", "node"),
            Operation("move-document", new() { ["documentId"] = String(), ["path"] = String() }, "documentId", "path"),
            Operation("rename-document-key", new() { ["documentId"] = String(), ["stableKey"] = String() }, "documentId", "stableKey"),
            Operation("remove-document", new() { ["documentId"] = String() }, "documentId"))
    };

    internal static JsonObject Object(JsonObject properties, params string[] required) => new()
    {
        ["type"] = "object",
        ["properties"] = properties,
        ["required"] = new JsonArray([.. required.Select(value => (JsonNode?)JsonValue.Create(value))]),
        ["additionalProperties"] = false
    };

    internal static JsonObject Choice(params string[] choices) => new()
    {
        ["type"] = "string",
        ["enum"] = new JsonArray([.. choices.Select(value => (JsonNode?)JsonValue.Create(value))])
    };

    internal static JsonObject String() => new() { ["type"] = "string" };
    internal static JsonObject Integer() => new() { ["type"] = "integer", ["minimum"] = 0 };
    internal static JsonObject Array(JsonNode items) => new() { ["type"] = "array", ["items"] = items, ["maxItems"] = McpWorkspaceOperations.MaximumOperations };

    static JsonObject Operation(string operation, JsonObject properties, params string[] required)
    {
        properties["operation"] = Choice(operation);
        return Object(properties, ["operation", .. required]);
    }
}

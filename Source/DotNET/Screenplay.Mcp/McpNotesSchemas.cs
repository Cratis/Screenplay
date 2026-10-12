// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Mcp;

static class McpNotesSchemas
{
    static readonly string[] _tools = ["list-model-notes", "propose-model-notes"];
    static readonly Dictionary<string, Func<JsonObject>> _arguments = new(StringComparer.Ordinal)
    {
        ["anchor"] = Target,
        ["operations"] = () => BoundedArray(Operation(), 200, minimum: 1),
        ["kind"] = () => McpAstSchemas.Choice("question", "task", "note"),
        ["state"] = () => McpAstSchemas.Choice("open", "resolved", "orphaned", "all"),
        ["view"] = () => McpAstSchemas.Choice("items", "findings")
    };

    internal static JsonObject? Argument(string tool, string property)
    {
        if (!_tools.Contains(tool, StringComparer.Ordinal)) return null;
        return _arguments.TryGetValue(property, out var read) ? read() : null;
    }

    static JsonObject Target() => new()
    {
        ["type"] = "object",
        ["oneOf"] = new JsonArray(
            McpAstSchemas.Object(new JsonObject { ["semanticId"] = McpAstSchemas.String() }, "semanticId"),
            McpAstSchemas.Object(new JsonObject { ["application"] = new JsonObject { ["const"] = true, ["type"] = "boolean" } }, "application"),
            McpAstSchemas.Object(new JsonObject { ["address"] = McpAstSchemas.String(), ["kind"] = McpAstSchemas.String() }, "address", "kind"),
            McpToolSchemas.Address(),
            McpAstSchemas.Handle())
    };

    static JsonObject BoundedArray(JsonObject item, int maximum, int minimum = 0)
    {
        var array = McpAstSchemas.Array(item);
        array["maxItems"] = maximum;
        array["minItems"] = minimum;
        return array;
    }

    static JsonObject Reference() => McpAstSchemas.Object(
        new JsonObject
        {
            ["id"] = McpAstSchemas.String(),
            ["url"] = new JsonObject { ["type"] = "string", ["format"] = "uri", ["pattern"] = "^https?://" }
        },
        "id");

    static JsonObject Resolution() => McpAstSchemas.Object(
        new JsonObject
        {
            ["by"] = McpAstSchemas.Choice("specification", "description", "decision", "external-decision", "dismissed"),
            ["text"] = McpAstSchemas.String(),
            ["author"] = McpAstSchemas.String(),
            ["semanticId"] = McpAstSchemas.String(),
            ["ref"] = Reference()
        },
        "by",
        "text",
        "author");

    static JsonObject Operation()
    {
        var schema = McpAstSchemas.Object(
            new JsonObject
            {
                ["operation"] = McpAstSchemas.Choice("add", "edit", "resolve", "reopen", "reanchor", "remove"),
                ["id"] = McpAstSchemas.String(),
                ["kind"] = McpAstSchemas.Choice("question", "task", "note"),
                ["text"] = McpAstSchemas.String(),
                ["author"] = McpAstSchemas.String(),
                ["refs"] = BoundedArray(Reference(), 100),
                ["anchor"] = Target(),
                ["target"] = Target(),
                ["resolution"] = Resolution()
            },
            "operation");
        schema["description"] = "add requires kind/text/author and exactly anchor or target; other operations require id. resolve requires resolution (server supplies at); reanchor requires exactly anchor or target. IDs and createdAt are server generated. Text is nonempty, trimmed, at most 16384 characters. No authoring-key or document anchors.";
        return schema;
    }
}

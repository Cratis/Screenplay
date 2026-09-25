// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp;

static class McpToolSchemas
{
    internal static JsonObject For(McpToolDefinition tool)
    {
        var properties = new JsonObject();
        foreach (var property in tool.Properties)
        {
            properties[property] = Argument(tool.Name, property);
        }

        var schema = McpAstSchemas.Object(properties, tool.Required);
        if (tool.Name == "propose")
        {
            schema["oneOf"] = new JsonArray(
                new JsonObject { ["required"] = new JsonArray("operations") },
                new JsonObject { ["required"] = new JsonArray("operation") });
        }

        if (tool.Name == "expand-layout")
        {
            schema["if"] = new JsonObject { ["properties"] = new JsonObject { ["validation"] = new JsonObject { ["const"] = "Authoring" } }, ["required"] = new JsonArray("validation") };
            schema["then"] = new JsonObject { ["required"] = new JsonArray("formatting") };
        }

        if (tool.Name == "read-workspace")
        {
            schema["if"] = new JsonObject { ["properties"] = new JsonObject { ["view"] = new JsonObject { ["const"] = "executable-model" } }, ["required"] = new JsonArray("view") };
            schema["then"] = new JsonObject { ["properties"] = new JsonObject { ["limit"] = Limit(192 * 1024) } };
            schema["else"] = new JsonObject { ["properties"] = new JsonObject { ["limit"] = Limit(200) } };
        }

        if (tool.Name == "read-proposal")
        {
            schema["if"] = new JsonObject { ["properties"] = new JsonObject { ["view"] = McpAstSchemas.Choice("before", "after") }, ["required"] = new JsonArray("view") };
            schema["then"] = new JsonObject { ["required"] = new JsonArray("documentId") };
            schema["else"] = new JsonObject { ["properties"] = new JsonObject { ["limit"] = Limit(200) } };
        }

        return schema;
    }

    internal static JsonObject Argument(string tool, string property) => property switch
    {
        "includeContent" => new() { ["type"] = "boolean", ["default"] = false },
        "descendants" => new() { ["type"] = "boolean", ["default"] = tool != "dependencies" },
        "offset" => McpAstSchemas.Integer(),
        "limit" => Limit(tool == "export-workspace" || tool == "read-proposal" || tool == "read-document" || tool == "merged-document" || tool == "workspace-state" || tool == "read-workspace" ? 192 * 1024 : 200),
        "operations" when tool == "propose-ast" => McpAstSchemas.Array(McpAstSchemas.Operations()),
        "operations" => McpAstSchemas.Array(JsonNode.Parse(McpWorkspaceOperations.Schema.GetRawText())!),
        "documents" => McpAstSchemas.Array(McpAstSchemas.Documents()),
        "target" or "subject" => McpAstSchemas.Handle(),
        "semanticRenames" or "eventRenames" => McpAstSchemas.Array(Rename()),
        "retiredSemanticAddresses" or "retiredEventAddresses" => McpAstSchemas.Array(Address()),
        "formatting" when tool == "propose-repair" => McpAstSchemas.Choice("CanonicalizeTouchedDocuments"),
        "formatting" => McpAstSchemas.Choice("CanonicalizeTouchedDocuments", "PreserveExactSource", "PreserveTrivia"),
        "validation" => McpAstSchemas.Choice("Authoring", "Executable"),
        "referencePolicy" => McpAstSchemas.Choice("Safe", "Draft"),
        "view" when tool == "workspace-state" => McpAstSchemas.Choice("status", "persisted", "before", "after"),
        "layout" => McpAstSchemas.Choice("single", "module", "feature", "slice"),
        "match" => McpAstSchemas.Choice("exact", "prefix", "contains"),
        "direction" => McpAstSchemas.Choice("incoming", "outgoing"),
        "view" when tool == "describe-application" => McpAstSchemas.Choice("summary", "children", "declarations"),
        "view" when tool == "declaration-details" => McpAstSchemas.Choice("summary", "properties", "occurrences", "commands", "specifications", "produces", "values", "syntax"),
        "view" when tool == "merged-document" => McpAstSchemas.Choice("source", "syntax", "both"),
        "view" when tool == "read-workspace" => McpAstSchemas.Choice("documents", "semantics", "eventContracts", "diagnostics", "executable-diagnostics", "implementation-requirements", "typed-contexts", "source-map", "repairs", "executable-model"),
        "view" when tool == "read-proposal" => McpAstSchemas.Choice("changes", "before", "after", "diagnostics", "executable-diagnostics", "implementation-requirements", "typed-contexts", "dropped-comments"),
        "view" when tool == "read-ast" => McpAstSchemas.Choice("nodes", "children"),
        _ => McpAstSchemas.String()
    };

    static JsonObject Limit(int maximum) => new() { ["type"] = "integer", ["minimum"] = 1, ["maximum"] = maximum };

    static JsonObject Address()
    {
        var part = new JsonObject
        {
            ["kind"] = McpAstSchemas.Choice([.. Enum.GetNames<SemanticAddressPartKind>().Where(name => name != nameof(SemanticAddressPartKind.Unknown))]),
            ["key"] = McpAstSchemas.String()
        };
        var parts = McpAstSchemas.Array(McpAstSchemas.Object(part, "kind", "key"));
        parts["minItems"] = 1;
        parts["maxItems"] = 128;
        var address = new JsonObject
        {
            ["kind"] = McpAstSchemas.Choice([.. Enum.GetNames<SemanticKind>().Where(name => name != nameof(SemanticKind.Unknown))]),
            ["parts"] = parts
        };
        return McpAstSchemas.Object(address, "kind", "parts");
    }

    static JsonObject Rename()
    {
        var properties = new JsonObject
        {
            ["previousAddress"] = Address(),
            ["currentAddress"] = Address()
        };
        return McpAstSchemas.Object(properties, "previousAddress", "currentAddress");
    }
}

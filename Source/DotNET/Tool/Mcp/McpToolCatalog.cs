// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpToolCatalog
{
    static readonly McpToolDefinition[] _tools =
    [
        new("describe-application", "Describe modules, features, slices, commands, specifications, and unresolved indexed references from the full syntax tree.", [], []),
        new("find-declaration", "Find declarations by exact short or dotted address and optional kind; returns source paths, locations, and concrete syntax.", ["name"], ["name", "kind"]),
        new("find-references", "Resolve indexed syntax references to an exact declaration address and kind. Ambiguous references are reported separately, never counted as resolved.", ["address", "kind"], ["address", "kind"]),
        new("merged-document", "Return the merged full AST and canonical .play text; diagnostics flag invalid or partial syntax.", [], []),
        new("diagnostics", "Return full-syntax parser, merge, and reference diagnostics (not executable-subset diagnostics).", [], []),
        new("open-workspace", "Open exact disk documents or reopen canonical workspaceJson, preserving its identity catalog. Returned workspaceJson must be saved to retain identities across sessions.", [], ["applicationName", "workspaceJson"]),
        new("propose", "Propose update-slice-description, move-document, add-document, replace-document, or remove-document. Whole documents use strict UTF-8 bytesBase64, never text patches. Pure; returns exact before/after documents and typed conflicts.", ["expectedRevision", "expectedCatalogRevision", "operation"], ["expectedRevision", "expectedCatalogRevision", "operation", "semanticId", "expectedDescription", "description", "documentId", "path", "stableKey", "bytesBase64"]),
        new("expand-layout", "Propose module/feature/slice files using PlayFileWriter. Requires syntax round-trip and semantic binding; does not write files.", ["expectedRevision", "expectedCatalogRevision"], ["expectedRevision", "expectedCatalogRevision"]),
        new("apply", "Apply only a server-produced proposal after revision and exact disk checks. Writes files with rollback; inspect RecoveryRequired and retained backup paths on failure.", ["proposalId", "expectedRevision", "expectedCatalogRevision"], ["proposalId", "expectedRevision", "expectedCatalogRevision"])
    ];

    internal static IEnumerable<object> Describe() => _tools.Select(tool => new
    {
        tool.Name,
        tool.Description,
        inputSchema = new
        {
            type = "object",
            properties = tool.Properties.ToDictionary(name => name, _ => new { type = "string" }, StringComparer.Ordinal),
            required = tool.Required,
            additionalProperties = false
        },
        annotations = new { readOnlyHint = tool.Name != "apply", destructiveHint = tool.Name == "apply", openWorldHint = false }
    });

    internal static void Validate(string name, JsonElement arguments)
    {
        var tool = _tools.SingleOrDefault(tool => tool.Name == name) ?? throw new McpFailure($"Unknown tool '{name}'.", -32602);
        if (arguments.ValueKind != JsonValueKind.Object)
        {
            throw new McpFailure("Tool arguments must be an object.", -32602);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in arguments.EnumerateObject())
        {
            if (!seen.Add(property.Name) || !tool.Properties.Contains(property.Name, StringComparer.Ordinal) || property.Value.ValueKind != JsonValueKind.String)
            {
                throw new McpFailure($"Unexpected, duplicate, or non-string argument '{property.Name}'.", -32602);
            }
        }

        foreach (var required in tool.Required)
        {
            _ = McpJson.RequiredString(arguments, required);
        }
    }
}

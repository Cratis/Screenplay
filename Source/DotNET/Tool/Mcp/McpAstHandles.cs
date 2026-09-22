// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpAstHandles
{
    internal static object Describe(WorkspaceNodeHandle handle) => new
    {
        revision = handle.Revision.ToString(),
        documentId = handle.Document.ToString(),
        path = handle.Path
    };

    internal static WorkspaceNodeHandle Read(JsonElement value)
    {
        McpJson.ValidateObject(value, ["revision", "documentId", "path"], ["revision", "documentId", "path"]);
        return new(
            WorkspaceRevision.Parse(McpJson.RequiredString(value, "revision")),
            DocumentId.Parse(McpJson.RequiredString(value, "documentId")),
            McpJson.RequiredString(value, "path"));
    }

    internal static object Describe(WorkspaceSyntaxEntry entry, bool includeContent, int? childCount = null) => new
    {
        handle = Describe(entry.Handle),
        parent = entry.Parent is null ? null : Describe(entry.Parent),
        entry.Member,
        entry.Index,
        entry.Kind,
        name = entry.Node.GetType().GetProperty("Name")?.GetValue(entry.Node) as string,
        childCount,
        entry.Location,
        semanticId = entry.SemanticId?.ToString(),
        eventContractId = entry.EventContractId?.ToString(),
        address = entry.Address is null ? null : McpSemanticAddresses.Describe(entry.Address),
        node = includeContent ? (object)SyntaxJson.Serialize(entry.Node) : null
    };
}

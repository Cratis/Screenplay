// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpWorkspaceOperations
{
    internal static ImmutableArray<WorkspaceOperation> Read(JsonElement arguments)
    {
        var operation = McpJson.RequiredString(arguments, "operation");
        var admitted = operation switch
        {
            "update-slice-description" => (WorkspaceOperation)new UpdateSliceDescription
            {
                SemanticId = SemanticId.Parse(McpJson.RequiredString(arguments, "semanticId")),
                ExpectedCurrentDescription = McpJson.RequiredString(arguments, "expectedDescription"),
                NewDescription = McpJson.RequiredString(arguments, "description")
            },
            "move-document" => new MoveWorkspaceDocument
            {
                Document = DocumentId.Parse(McpJson.RequiredString(arguments, "documentId")),
                Path = PortablePlayPath.Parse(McpJson.RequiredString(arguments, "path"))
            },
            "add-document" => new AddWorkspaceDocument
            {
                StableKey = McpJson.RequiredString(arguments, "stableKey"),
                Path = PortablePlayPath.Parse(McpJson.RequiredString(arguments, "path")),
                Bytes = Bytes(arguments)
            },
            "replace-document" => new ReplaceWorkspaceDocument
            {
                Document = DocumentId.Parse(McpJson.RequiredString(arguments, "documentId")),
                Bytes = Bytes(arguments)
            },
            "remove-document" => new RemoveWorkspaceDocument
            {
                Document = DocumentId.Parse(McpJson.RequiredString(arguments, "documentId"))
            },
            _ => throw new McpFailure($"Unsupported operation '{operation}'.", -32602)
        };
        return [admitted];
    }

    static ImmutableArray<byte> Bytes(JsonElement arguments)
    {
        var bytes = Convert.FromBase64String(McpJson.RequiredString(arguments, "bytesBase64"));
        var document = WorkspaceDocument.Create("admission", PortablePlayPath.Parse("admission.play"), bytes);
        McpRoot.CheckDocuments([document]);
        return document.Bytes;
    }
}

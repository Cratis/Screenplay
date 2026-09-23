// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpAstOperations
{
    internal static ImmutableArray<WorkspaceAstOperation> Read(JsonElement arguments, WorkspaceSyntaxIndex index) =>
        [.. Values(arguments, "operations").Select(value => ReadOperation(value, index))];

    internal static ImmutableArray<WorkspaceOperation> Documents(JsonElement arguments) =>
        [.. Values(arguments, "documents").Select(Document)];

    static WorkspaceAstOperation ReadOperation(JsonElement value, WorkspaceSyntaxIndex index)
    {
        var operation = McpJson.RequiredString(value, "operation");
        var fields = operation switch
        {
            "add" => new[] { "operation", "parent", "expectedParent", "member", "node", "index" },
            "replace" => ["operation", "target", "expected", "node"],
            "remove" => ["operation", "target", "expected"],
            "move" => ["operation", "target", "expected", "parent", "expectedParent", "member", "index"],
            _ => throw new McpFailure($"Unknown AST operation '{operation}'.", -32602)
        };
        McpJson.ValidateObject(value, fields, ["operation"]);
        return operation switch
        {
            "add" => new AddWorkspaceNode(
                Handle(value, "parent"),
                Expected(value, "parent", "expectedParent", index),
                McpJson.RequiredString(value, "member"),
                Node(value, "node"),
                Position(value)),
            "replace" => new ReplaceWorkspaceNode(Handle(value, "target"), Expected(value, "target", "expected", index), Node(value, "node")),
            "remove" => new RemoveWorkspaceNode(Handle(value, "target"), Expected(value, "target", "expected", index)),
            "move" => new MoveWorkspaceNode(
                Handle(value, "target"),
                Expected(value, "target", "expected", index),
                Handle(value, "parent"),
                Expected(value, "parent", "expectedParent", index),
                McpJson.RequiredString(value, "member"),
                Position(value)),
            _ => throw new McpFailure($"Unknown AST operation '{operation}'.", -32602)
        };
    }

    static WorkspaceOperation Document(JsonElement value)
    {
        var operation = McpJson.RequiredString(value, "operation");
        var fields = operation switch
        {
            "create-document" => new[] { "operation", "stableKey", "path", "node", "encoding" },
            "replace-document" => ["operation", "documentId", "node"],
            "move-document" => ["operation", "documentId", "path"],
            "rename-document-key" => ["operation", "documentId", "stableKey"],
            "remove-document" => ["operation", "documentId"],
            _ => throw new McpFailure($"Unknown authoring document operation '{operation}'.", -32602)
        };
        McpJson.ValidateObject(value, fields, ["operation"]);
        return operation switch
        {
            "create-document" => new CreateWorkspaceSyntaxDocument(
                McpJson.RequiredString(value, "stableKey"),
                PortablePlayPath.Parse(McpJson.RequiredString(value, "path")),
                Node(value, "node") as ApplicationSyntax ?? throw new McpFailure("A new document requires ApplicationSyntax.", -32602),
                McpJson.Enumeration(value, "encoding", WorkspaceTextEncoding.Utf8)),
            "replace-document" => new ReplaceWorkspaceSyntaxDocument(
                DocumentId.Parse(McpJson.RequiredString(value, "documentId")),
                Node(value, "node") as ApplicationSyntax ?? throw new McpFailure("A document replacement requires ApplicationSyntax.", -32602)),
            "move-document" => new MoveWorkspaceDocument
            {
                Document = DocumentId.Parse(McpJson.RequiredString(value, "documentId")),
                Path = PortablePlayPath.Parse(McpJson.RequiredString(value, "path"))
            },
            "rename-document-key" => new RenameWorkspaceDocument
            {
                Document = DocumentId.Parse(McpJson.RequiredString(value, "documentId")),
                StableKey = McpJson.RequiredString(value, "stableKey")
            },
            "remove-document" => new RemoveWorkspaceDocument { Document = DocumentId.Parse(McpJson.RequiredString(value, "documentId")) },
            _ => throw new McpFailure($"Unknown authoring document operation '{operation}'.", -32602)
        };
    }

    static JsonElement.ArrayEnumerator Values(JsonElement arguments, string name)
    {
        if (!arguments.TryGetProperty(name, out var values))
        {
            return McpJson.EmptyArray.EnumerateArray();
        }

        if (values.ValueKind != JsonValueKind.Array || values.GetArrayLength() > McpWorkspaceOperations.MaximumOperations)
        {
            throw new McpFailure($"'{name}' must contain at most {McpWorkspaceOperations.MaximumOperations} entries.", -32602);
        }

        return values.EnumerateArray();
    }

    static WorkspaceNodeHandle Handle(JsonElement value, string name) => value.TryGetProperty(name, out var handle)
        ? McpAstHandles.Read(handle) : throw new McpFailure($"'{name}' is required.", -32602);

    static SyntaxNode Node(JsonElement value, string name) => value.TryGetProperty(name, out var node)
        ? SyntaxJson.Deserialize(node) : throw new McpFailure($"'{name}' is required.", -32602);

    static SyntaxNode Expected(JsonElement value, string handle, string expectation, WorkspaceSyntaxIndex index) =>
        value.TryGetProperty(expectation, out var expected)
            ? SyntaxJson.Deserialize(expected)
            : index.Find(Handle(value, handle))?.Node ?? throw new McpFailure("UnknownNode: handle is not in the original workspace revision.");

    static int? Position(JsonElement value) => value.TryGetProperty("index", out _) ? McpJson.Integer(value, "index", 0, 0, int.MaxValue) : null;
}

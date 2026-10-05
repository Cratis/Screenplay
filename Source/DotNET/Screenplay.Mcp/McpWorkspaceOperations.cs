// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpWorkspaceOperations
{
    internal const int MaximumOperations = 256;
    static readonly McpOperationDefinition[] _definitions =
    [
        new("update-slice-description", ["semanticId", "expectedDescription", "description"]),
        new("move-document", ["documentId", "path"]),
        new("rename-document-key", ["documentId", "stableKey"]),
        new("add-document", ["path", "bytesBase64"], ["stableKey"]),
        new("replace-document", ["documentId", "bytesBase64"]),
        new("remove-document", ["documentId"])
    ];

    internal static JsonElement Schema => JsonSerializer.SerializeToElement(new
    {
        oneOf = _definitions.Select(definition => new
        {
            type = "object",
            properties = definition.Fields.ToDictionary(member => member, _ => (object)new { type = "string" }, StringComparer.Ordinal)
                .Concat(definition.Optional.Select(member => new KeyValuePair<string, object>(member, new { type = "string" })))
                .Append(new KeyValuePair<string, object>("operation", new { type = "string", @enum = new[] { definition.Name } })).ToDictionary(),
            required = (string[])["operation", .. definition.Fields],
            additionalProperties = false
        })
    });

    internal static ImmutableArray<WorkspaceOperation> Read(JsonElement arguments)
    {
        if (!arguments.TryGetProperty("operations", out var operations))
        {
            // Preserve the original single-operation surface, while validating its operation-specific fields.
            var operation = McpJson.RequiredString(arguments, "operation");
            var definition = Definition(operation);
            McpJson.ValidateObject(
                arguments,
                ["expectedRevision", "expectedCatalogRevision", "includeContent", "operation", .. McpToolCatalog.IdentityProperties, .. definition.Fields, .. definition.Optional],
                ["operation"]);
            var payload = JsonSerializer.SerializeToElement(arguments.EnumerateObject()
                .Where(property => property.Name == "operation" || definition.Fields.Contains(property.Name, StringComparer.Ordinal))
                .ToDictionary(property => property.Name, property => property.Value, StringComparer.Ordinal));
            return [ReadOperation(payload)];
        }

        if (arguments.TryGetProperty("operation", out _) || operations.ValueKind != JsonValueKind.Array || operations.GetArrayLength() is < 1 or > MaximumOperations)
        {
            throw new McpFailure($"Supply either one operation or an operations array with 1–{MaximumOperations} entries.", -32602);
        }

        McpJson.ValidateObject(
            arguments,
            ["expectedRevision", "expectedCatalogRevision", "includeContent", "operations", .. McpToolCatalog.IdentityProperties],
            ["operations"]);
        return [.. operations.EnumerateArray().Select(ReadOperation)];
    }

    static WorkspaceOperation ReadOperation(JsonElement value)
    {
        var operation = McpJson.RequiredString(value, "operation");
        var definition = Definition(operation);
        McpJson.ValidateObject(value, ["operation", .. definition.Fields, .. definition.Optional], ["operation", .. definition.Fields]);
        foreach (var field in definition.Fields)
        {
            _ = McpJson.RequiredString(value, field);
        }

        return operation switch
        {
            "update-slice-description" => new UpdateSliceDescription
            {
                SemanticId = SemanticId.Parse(McpJson.RequiredString(value, "semanticId")),
                ExpectedCurrentDescription = McpJson.RequiredString(value, "expectedDescription"),
                NewDescription = McpJson.RequiredString(value, "description")
            },
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
            "add-document" => AddDocument(value),
            "replace-document" => new ReplaceWorkspaceDocument
            {
                Document = DocumentId.Parse(McpJson.RequiredString(value, "documentId")),
                Bytes = Bytes(value)
            },
            "remove-document" => new RemoveWorkspaceDocument { Document = DocumentId.Parse(McpJson.RequiredString(value, "documentId")) },
            _ => throw new McpFailure($"Unsupported operation '{operation}'.", -32602)
        };
    }

    static McpOperationDefinition Definition(string operation) => _definitions.SingleOrDefault(definition => definition.Name == operation)
        ?? throw new McpFailure($"Unsupported operation '{operation}'.", -32602);

    // A stable key is derived from the path when the caller does not supply one; admission rejects collisions,
    // and rename-document-key remains the explicit way to change a key later.
    static AddWorkspaceDocument AddDocument(JsonElement value)
    {
        var path = PortablePlayPath.Parse(McpJson.RequiredString(value, "path"));
        return new AddWorkspaceDocument
        {
            StableKey = value.TryGetProperty("stableKey", out var supplied) && supplied.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(supplied.GetString())
                ? supplied.GetString()!
                : DeriveStableKey(path),
            Path = path,
            Bytes = Bytes(value)
        };
    }

    static string DeriveStableKey(PortablePlayPath path)
    {
        var value = path.Value.Replace('\\', '/');
        var fileName = value[(value.LastIndexOf('/') + 1)..];
        return fileName.EndsWith(".play", StringComparison.Ordinal) && fileName.Length > ".play".Length
            ? fileName[..^".play".Length]
            : throw new McpFailure("Supply a stableKey; the path has no derivable document name.", -32602);
    }

    static ImmutableArray<byte> Bytes(JsonElement arguments)
    {
        var bytes = Convert.FromBase64String(McpJson.RequiredString(arguments, "bytesBase64"));
        var document = WorkspaceDocument.Create("admission", PortablePlayPath.Parse("admission.play"), bytes);
        McpRoot.CheckDocuments([document]);
        return document.Bytes;
    }
}

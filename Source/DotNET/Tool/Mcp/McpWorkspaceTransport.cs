// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpWorkspaceTransport
{
    internal static object Describe(ScreenplayWorkspace workspace)
    {
        var bytes = ScreenplayWorkspaceSerializer.Serialize(workspace);
        var json = Encoding.UTF8.GetString(bytes);
        if (bytes.Length > 2 * McpRoot.MaximumBytes || JsonSerializer.Serialize(json).Length > McpConnection.MaximumRequestCharacters - 4096)
        {
            throw new McpFailure("Workspace transport exceeds the bounded reopening envelope; choose a smaller root.");
        }

        return new
        {
            revision = workspace.Revision.ToString(),
            catalogRevision = workspace.IdentityCatalog.Revision.ToString(),
            workspaceJson = json,
            semanticSuccess = workspace.Compilation.Success,
            diagnostics = workspace.Compilation.Diagnostics,
            documentCount = workspace.Documents.Length
        };
    }

    internal static ScreenplayWorkspace Restore(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > 2 * McpRoot.MaximumBytes)
        {
            throw new McpFailure("Workspace transport exceeds two MiB.");
        }

        // Bound decoded documents before the canonical serializer invokes semantic compilation.
        using var transport = JsonDocument.Parse(json);
        var documents = transport.RootElement.GetProperty("documents");
        if (documents.ValueKind != JsonValueKind.Array || documents.GetArrayLength() > McpRoot.MaximumFiles)
        {
            throw new McpFailure("Workspace documents must be an array of at most 128 entries.");
        }

        var admitted = documents.EnumerateArray().Select(document => WorkspaceDocument.Create(
            document.GetProperty("stableKey").GetString()!,
            PortablePlayPath.Parse(document.GetProperty("path").GetString()!),
            document.GetProperty("bytes").GetBytesFromBase64())).ToImmutableArray();
        McpRoot.CheckDocuments(admitted);
        return ScreenplayWorkspaceSerializer.Deserialize(Encoding.UTF8.GetBytes(json));
    }
}

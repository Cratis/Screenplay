// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpWorkspaceTransport
{
    static readonly ConditionalWeakTable<ScreenplayWorkspace, byte[]> _admittedExports = [];

    internal static object Describe(ScreenplayWorkspace workspace, bool includeContent = false)
    {
        var diagnostics = workspace.Compilation.Diagnostics.ToArray();
        var source = workspace.Documents.IsEmpty ? null : McpWorkspaceAnalysis.For(workspace).Source.Compilation;
        return new
        {
            revision = workspace.Revision.ToString(),
            catalogRevision = workspace.IdentityCatalog.Revision.ToString(),
            workspaceJson = includeContent ? Export(workspace) : null,
            sourceSuccess = source?.Success,
            sourceDiagnosticCount = source?.Diagnostics.Count() ?? 0,
            semanticSuccess = workspace.Compilation.Success,
            executableReady = workspace.Compilation.Success,
            diagnosticCount = diagnostics.Length,
            diagnostics = includeContent ? diagnostics : null,
            documentCount = workspace.Documents.Length,
            sourceBytes = workspace.Documents.Sum(document => document.Bytes.Length),
            limits = new
            {
                maximumFiles = McpRoot.MaximumFiles,
                maximumSourceBytes = McpRoot.MaximumBytes,
                maximumFileBytes = McpRoot.MaximumFileBytes,
                maximumStructuredResponseBytes = McpJson.MaximumStructuredResponseBytes
            },
            identityPersistence = "root-local-on-apply",
            identityStatePath = ".screenplay/identities.json"
        };
    }

    internal static byte[] ExportBytes(ScreenplayWorkspace workspace) => _admittedExports.GetValue(workspace, AdmitExport);

    internal static ScreenplayWorkspace Restore(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > 2 * McpRoot.MaximumBytes)
        {
            throw new McpFailure($"Workspace transport exceeds {2 * McpRoot.MaximumBytes} bytes.");
        }

        // Bound decoded documents before the canonical serializer invokes semantic compilation.
        using var transport = JsonDocument.Parse(json);
        var documents = transport.RootElement.GetProperty("documents");
        if (documents.ValueKind != JsonValueKind.Array || documents.GetArrayLength() > McpRoot.MaximumFiles)
        {
            throw new McpFailure($"Workspace documents must be an array of at most {McpRoot.MaximumFiles} entries.");
        }

        var admitted = documents.EnumerateArray().Select(document => WorkspaceDocument.Create(
            document.GetProperty("stableKey").GetString()!,
            PortablePlayPath.Parse(document.GetProperty("path").GetString()!),
            document.GetProperty("bytes").GetBytesFromBase64())).ToImmutableArray();
        McpRoot.CheckDocuments(admitted, allowEmpty: true);
        return ScreenplayWorkspaceSerializer.Deserialize(Encoding.UTF8.GetBytes(json));
    }

    static byte[] AdmitExport(ScreenplayWorkspace workspace)
    {
        var bytes = McpWorkspaceAnalysis.For(workspace).ExportBytes;
        if (bytes.Length > 2 * McpRoot.MaximumBytes || JsonSerializer.Serialize(Encoding.UTF8.GetString(bytes)).Length > McpConnection.MaximumRequestCharacters - 4096)
        {
            throw new McpFailure("Workspace transport exceeds the bounded reopening envelope; choose a smaller root.");
        }

        return bytes;
    }

    static string Export(ScreenplayWorkspace workspace) => Encoding.UTF8.GetString(ExportBytes(workspace));
}

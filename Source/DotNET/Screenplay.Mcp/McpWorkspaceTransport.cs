// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpWorkspaceTransport
{
    static readonly ConditionalWeakTable<ScreenplayWorkspace, byte[]> _admittedExports = [];

    internal static object Describe(ScreenplayWorkspace workspace, bool includeContent = false)
    {
        var diagnostics = workspace.Compilation.Diagnostics.ToArray();
        var source = workspace.Documents.IsEmpty ? null : McpWorkspaceAnalysis.For(workspace).Source.Compilation;
        var sourceDiagnosticCount = source?.Diagnostics.Count() ?? 0;
        return new
        {
            revision = workspace.Revision.ToString(),
            catalogRevision = workspace.IdentityCatalog.Revision.ToString(),
            applicationIdentity = workspace.IdentityCatalog.Application.ToString(),
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
            identityStatePath = ".screenplay/identities.json",
            containerIdentities = ContainerIdentities(workspace),
            readiness = Readiness(workspace, source?.Success ?? true, sourceDiagnosticCount, diagnostics.Length)
        };
    }

    internal static object? ContainerIdentities(ScreenplayWorkspace workspace)
    {
        var index = McpWorkspaceAnalysis.For(workspace).Syntax;
        var containers = new WorkspaceContainerIdentities(index);
        var issues = containers.Issues;
        if (containers.Complete && issues.IsEmpty) return null;
        return new
        {
            complete = containers.Complete,
            containers = issues.Select(issue => new { address = McpSemanticAddresses.Describe(issue.Address), issue.Reason, issue.Assigned }),
            documents = index.UnparsedDocuments.Select(document => new { path = document.Path.Value, reason = "unparsed" })
                .Concat(index.UnresolvedPlacementDocuments.Select(document => new { path = document.Path.Value, reason = "unplaced" })),
            note = containers.Complete ? null : "No new identities are assigned until every document parses and places."
        };
    }

    internal static byte[] ExportBytes(ScreenplayWorkspace workspace) => _admittedExports.GetValue(workspace, AdmitExport);

    /// <summary>
    /// Restores a workspace transport; when it is exactly the export of <paramref name="known"/>, returns that workspace instead of compiling it again.
    /// </summary>
    /// <param name="json">The workspace transport.</param>
    /// <param name="known">A workspace whose canonical export may equal the transport, or null.</param>
    /// <returns>The restored workspace.</returns>
    internal static ScreenplayWorkspace Restore(string json, ScreenplayWorkspace? known)
    {
        Admit(json);
        var bytes = Encoding.UTF8.GetBytes(json);
        return known is not null && bytes.AsSpan().SequenceEqual(ExportBytes(known)) ? known : ScreenplayWorkspaceSerializer.Deserialize(bytes);
    }

    internal static ScreenplayWorkspace Restore(string json)
    {
        Admit(json);
        return ScreenplayWorkspaceSerializer.Deserialize(Encoding.UTF8.GetBytes(json));
    }

    static void Admit(string json)
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

    // Authoring acceptance and executable readiness are separate verdicts: a full-language model the compiler
    // accepts authoring-side reports executable debt without being called broken, and an empty root is a valid start.
    static object Readiness(ScreenplayWorkspace workspace, bool sourceSuccess, int authoringDiagnosticCount, int executableDiagnosticCount)
    {
        var empty = workspace.Documents.IsEmpty;
        var authoringAccepted = empty || sourceSuccess;
        var state = "invalid";
        if (empty)
        {
            state = "empty";
        }
        else if (authoringAccepted)
        {
            state = "authored";
        }

        string? note = null;
        if (empty)
        {
            note = "An empty workspace is a valid starting point; create the first document with propose-source or propose-ast.";
        }
        else if (authoringAccepted && !workspace.Compilation.Success)
        {
            note = "Authoring is accepted; executableReady describes the current ESM executable subset only.";
        }

        return new
        {
            state,
            authoringAccepted,
            executableReady = workspace.Compilation.Success,
            authoringDiagnosticCount,
            executableDiagnosticCount,
            note
        };
    }
}

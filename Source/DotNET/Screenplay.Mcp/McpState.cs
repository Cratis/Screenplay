// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed record McpState(string ApplicationName, SemanticIdentityCatalog Catalog, ImmutableArray<WorkspaceDocument> Mappings)
{
    internal const string FileName = "identities.json";

    internal static byte[] Serialize(ScreenplayWorkspace workspace) => new McpState(
        workspace.ApplicationName,
        workspace.IdentityCatalog,
        [.. workspace.Documents.Select(document => WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, []))]).Serialize();

    internal static McpState Deserialize(byte[] bytes)
    {
        try
        {
            using var json = JsonDocument.Parse(bytes);
            var value = json.RootElement;
            if (value.GetProperty("schema").GetString() != "cratis.screenplay.mcp-identities" || value.GetProperty("schemaVersion").GetInt32() != 1)
            {
                throw new McpFailure("Unsupported identity-state schema or version.");
            }

            var catalog = SemanticIdentityCatalogSerializer.Deserialize(Encoding.UTF8.GetBytes(value.GetProperty("identityCatalog").GetRawText()));
            var documents = value.GetProperty("documents");
            if (documents.GetArrayLength() > McpRoot.MaximumFiles)
            {
                throw new McpFailure("Identity-state document count exceeds the workspace bound.");
            }

            var mappings = documents.EnumerateArray().Select(document => WorkspaceDocument.Create(
                DocumentId.Parse(document.GetProperty("id").GetString()!),
                document.GetProperty("stableKey").GetString()!,
                PortablePlayPath.Parse(document.GetProperty("path").GetString()!),
                [])).ToImmutableArray();
            var state = new McpState(value.GetProperty("applicationName").GetString()!, catalog, mappings);
            if (string.IsNullOrWhiteSpace(state.ApplicationName) ||
                mappings.Select(document => document.Id).Distinct().Count() != mappings.Length ||
                mappings.Select(document => document.StableKey).Distinct(StringComparer.Ordinal).Count() != mappings.Length ||
                mappings.Select(document => document.Path).Distinct(PortablePlayPath.CollisionComparer).Count() != mappings.Length ||
                mappings.Any(document => catalog.Documents.Any(assignment => assignment.Key == document.StableKey && assignment.Id != document.Id)) ||
                !bytes.AsSpan().SequenceEqual(state.Serialize()))
            {
                throw new McpFailure("Identity state is noncanonical or has inconsistent document mappings.");
            }

            return state;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            throw new McpFailure($"IdentityStateConflict: .screenplay/identities.json is corrupt or unsupported. Restore a known-good identity file; do not delete it to regenerate IDs. {exception.Message}");
        }
    }

    internal ScreenplayWorkspace Open(McpRoot root)
    {
        foreach (var mapping in Mappings)
        {
            _ = root.PathFor(mapping.Path);
        }

        var actual = root.Read(allowEmpty: true).ToDictionary(document => document.Path.Value, StringComparer.Ordinal);
        if (actual.Count != Mappings.Length || Mappings.Any(mapping => !actual.ContainsKey(mapping.Path.Value)))
        {
            throw new McpFailure("IdentityMappingConflict: mapped .play files are missing, renamed, or accompanied by unmapped files. Restore the mapped paths, then use an MCP proposal to move/add/delete documents explicitly.");
        }

        var documents = Mappings.Select(mapping => WorkspaceDocument.Create(
            mapping.Id,
            mapping.StableKey,
            mapping.Path,
            actual[mapping.Path.Value].Bytes.AsSpan())).ToImmutableArray();
        ScreenplayWorkspace workspace;
        try
        {
            workspace = documents.IsEmpty
                ? ScreenplayWorkspace.CreateEmpty(Catalog.Application, ApplicationName)
                : CreateWithAttachments(root, documents);
        }
        catch (Exception exception) when (exception is InvalidScreenplayWorkspace or InvalidSemanticContract)
        {
            throw new McpFailure($"IdentityReconciliationRequired: current source cannot be admitted with the persisted catalog. Restore its declaration names and use explicit identity-change proposals. {exception.Message}");
        }
        var addresses = McpWorkspaceAnalysis.For(workspace).Syntax.Entries
            .Where(entry => entry.Address is not null).Select(entry => entry.Address).ToHashSet();

        // Failed binding retains the old catalog, so identical catalog bytes alone cannot prove source continuity.
        var missingAssignments = !workspace.Compilation.Success &&
            (Catalog.Semantics.Any(assignment => !addresses.Contains(assignment.Address)) ||
             Catalog.EventContracts.Any(assignment => !addresses.Contains(assignment.Address)));
        if (missingAssignments || !SemanticIdentityCatalogSerializer.Serialize(Catalog).AsSpan().SequenceEqual(SemanticIdentityCatalogSerializer.Serialize(workspace.IdentityCatalog)))
        {
            throw new McpFailure("IdentityReconciliationRequired: external source edits would change persisted identities. Restore the previous declaration names and apply an explicit semantic/event rename or retirement proposal. Identity state was not replaced.");
        }

        return workspace;
    }

    internal byte[] Serialize()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WriteString("schema", "cratis.screenplay.mcp-identities");
        writer.WriteNumber("schemaVersion", 1);
        writer.WriteString("applicationIdentity", Catalog.Application.ToString());
        writer.WriteString("applicationName", ApplicationName);
        writer.WritePropertyName("documents");
        writer.WriteStartArray();
        foreach (var document in Mappings.OrderBy(document => document.Id.ToString(), StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("id", document.Id.ToString());
            writer.WriteString("stableKey", document.StableKey);
            writer.WriteString("path", document.Path.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WritePropertyName("identityCatalog");
        writer.WriteRawValue(SemanticIdentityCatalogSerializer.Serialize(Catalog));
        writer.WriteEndObject();
        writer.Flush();
        if (buffer.WrittenCount > McpManagedFiles.MaximumStateBytes)
        {
            throw new McpFailure("Identity state exceeds the bounded metadata size.");
        }

        return buffer.WrittenSpan.ToArray();
    }

    ScreenplayWorkspace CreateWithAttachments(McpRoot root, ImmutableArray<WorkspaceDocument> documents)
    {
        var loaded = McpAttachmentContents.Load(root, documents);
        return ScreenplayWorkspace.Create(Catalog.Application, ApplicationName, documents, Catalog, loaded.Contents, loaded.Diagnostics);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Reads and writes a model root's <c>.screenplay/identities.json</c> (schema <c>cratis.screenplay.mcp-identities</c>, version 1).
/// </summary>
/// <remarks>
/// This is the only implementation of the format; the MCP server, <c>screenplay baseline</c> and other tools all use it.
/// Reading is strict: the bytes must be exactly the canonical serialization.
/// </remarks>
public static class PersistedIdentityStateSerializer
{
    /// <summary>
    /// The file name inside a model root's <c>.screenplay</c> directory.
    /// </summary>
    public const string FileName = "identities.json";

    /// <summary>
    /// The schema discriminator written in the file.
    /// </summary>
    public const string Schema = "cratis.screenplay.mcp-identities";

    /// <summary>
    /// The supported schema version.
    /// </summary>
    public const int SchemaVersion = 1;

    /// <summary>
    /// The maximum size of the serialized state in bytes.
    /// </summary>
    public const int MaximumBytes = ModelRootDirectory.MaximumBytes;

    /// <summary>
    /// Reads persisted identity state from its exact canonical bytes.
    /// </summary>
    /// <param name="bytes">The content of <c>.screenplay/identities.json</c>.</param>
    /// <returns>The application name, identity catalog and document mappings.</returns>
    /// <exception cref="InvalidPersistedIdentityState">The bytes are corrupt, noncanonical, inconsistent or of an unsupported schema.</exception>
    public static PersistedIdentityState Deserialize(ReadOnlySpan<byte> bytes)
    {
        try
        {
            using var json = JsonDocument.Parse(bytes.ToArray());
            var value = json.RootElement;
            if (value.GetProperty("schema").GetString() != Schema || value.GetProperty("schemaVersion").GetInt32() != SchemaVersion)
            {
                throw new InvalidPersistedIdentityState("Unsupported identity-state schema or version.");
            }

            var catalog = SemanticIdentityCatalogSerializer.Deserialize(Encoding.UTF8.GetBytes(value.GetProperty("identityCatalog").GetRawText()));
            var documents = value.GetProperty("documents");
            if (documents.GetArrayLength() > ModelRootDirectory.MaximumFiles)
            {
                throw new InvalidPersistedIdentityState("Identity-state document count exceeds the workspace bound.");
            }

            var mappings = documents.EnumerateArray().Select(document => new PersistedDocumentMapping(
                DocumentId.Parse(document.GetProperty("id").GetString()!),
                document.GetProperty("stableKey").GetString()!,
                PortablePlayPath.Parse(document.GetProperty("path").GetString()!))).ToImmutableArray();

            // Creating a workspace document validates the stable key exactly as the MCP state always has.
            _ = mappings.Select(mapping => WorkspaceDocument.Create(mapping.Id, mapping.StableKey, mapping.Path, [])).ToArray();
            var state = new PersistedIdentityState(value.GetProperty("applicationName").GetString()!, catalog, mappings);
            if (string.IsNullOrWhiteSpace(state.ApplicationName) ||
                mappings.Select(document => document.Id).Distinct().Count() != mappings.Length ||
                mappings.Select(document => document.StableKey).Distinct(StringComparer.Ordinal).Count() != mappings.Length ||
                mappings.Select(document => document.Path).Distinct(PortablePlayPath.CollisionComparer).Count() != mappings.Length ||
                mappings.Any(document => catalog.Documents.Any(assignment => assignment.Key == document.StableKey && assignment.Id != document.Id)) ||
                !bytes.SequenceEqual(Serialize(state)))
            {
                throw new InvalidPersistedIdentityState("Identity state is noncanonical or has inconsistent document mappings.");
            }

            return state;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException and not InvalidPersistedIdentityState)
        {
            throw new InvalidPersistedIdentityState(exception.Message, exception);
        }
    }

    /// <summary>
    /// Writes persisted identity state as its canonical bytes.
    /// </summary>
    /// <param name="state">The state to write.</param>
    /// <returns>The canonical UTF-8 bytes.</returns>
    /// <exception cref="InvalidPersistedIdentityState">The serialized state exceeds <see cref="MaximumBytes"/>.</exception>
    public static byte[] Serialize(PersistedIdentityState state)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        writer.WriteString("schema", Schema);
        writer.WriteNumber("schemaVersion", SchemaVersion);
        writer.WriteString("applicationIdentity", state.IdentityCatalog.Application.ToString());
        writer.WriteString("applicationName", state.ApplicationName);
        writer.WritePropertyName("documents");
        writer.WriteStartArray();
        foreach (var document in state.Documents.OrderBy(document => document.Id.ToString(), StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("id", document.Id.ToString());
            writer.WriteString("stableKey", document.StableKey);
            writer.WriteString("path", document.Path.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WritePropertyName("identityCatalog");
        writer.WriteRawValue(SemanticIdentityCatalogSerializer.Serialize(state.IdentityCatalog));
        writer.WriteEndObject();
        writer.Flush();
        if (buffer.WrittenCount > MaximumBytes)
        {
            throw new InvalidPersistedIdentityState("Identity state exceeds the bounded metadata size.");
        }

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Captures the persisted identity state of a workspace: its name, catalog and document mappings, without source.
    /// </summary>
    /// <param name="workspace">The workspace whose identities to persist.</param>
    /// <returns>The state that <see cref="Serialize"/> writes for the workspace.</returns>
    public static PersistedIdentityState From(ScreenplayWorkspace workspace) => new(
        workspace.ApplicationName,
        workspace.IdentityCatalog,
        [.. workspace.Documents.Select(document => new PersistedDocumentMapping(document.Id, document.StableKey, document.Path))]);
}

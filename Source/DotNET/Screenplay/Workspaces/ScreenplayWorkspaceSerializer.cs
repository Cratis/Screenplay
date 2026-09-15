// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Provides pure canonical version 1 transport of exact workspace sources and authoritative identities.
/// </summary>
/// <remarks>
/// Callers must bound input size before reading; this in-memory format is not an archive or a renderer plan.
/// Invalid-but-editable source retains its compilation diagnostics. Revisions detect mismatched content,
/// not authenticity, and callers must separately compare the revision with their expected workspace revision.
/// </remarks>
public static class ScreenplayWorkspaceSerializer
{
    const string Schema = "cratis.screenplay.workspace";
    const uint SchemaVersion = 1;

    /// <summary>
    /// Serializes a workspace to deterministic canonical UTF-8 JSON without a transport BOM or whitespace.
    /// </summary>
    /// <param name="workspace">The workspace whose catalog, identities, paths, and exact source bytes are preserved.</param>
    /// <returns>The canonical version 1 envelope, with source bytes encoded as base64 including any UTF-8 BOM.</returns>
    /// <exception cref="InvalidScreenplayWorkspace">The workspace is null or its catalog or revision is invalid.</exception>
    public static byte[] Serialize(ScreenplayWorkspace workspace)
    {
        if (workspace is null)
        {
            throw new InvalidScreenplayWorkspace("The workspace cannot be null.");
        }

        try
        {
            var catalog = SemanticIdentityCatalogSerializer.Serialize(workspace.IdentityCatalog);
            if (WorkspaceCanonicalRevision.Compute(workspace.ApplicationName, workspace.Documents, workspace.IdentityCatalog) != workspace.Revision)
            {
                throw new InvalidScreenplayWorkspace("The workspace revision does not match its exact content.");
            }

            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter(buffer, CanonicalJson.WriterOptions);
            writer.WriteStartObject();
            writer.WriteString("schema", Schema);
            writer.WriteNumber("schemaVersion", SchemaVersion);
            writer.WriteString("applicationIdentity", workspace.IdentityCatalog.Application.ToString());
            CanonicalJson.WriteString(writer, "applicationName", workspace.ApplicationName);
            writer.WriteString("revision", workspace.Revision.ToString());
            writer.WritePropertyName("identityCatalog");
            writer.WriteRawValue(catalog);
            writer.WritePropertyName("documents");
            writer.WriteStartArray();
            foreach (var document in workspace.Documents.OrderBy(value => value.Id.ToString(), StringComparer.Ordinal))
            {
                writer.WriteStartObject();
                writer.WriteString("id", document.Id.ToString());
                CanonicalJson.WriteString(writer, "stableKey", document.StableKey);
                CanonicalJson.WriteString(writer, "path", document.Path.Value);
                writer.WriteBase64String("bytes", document.Bytes.AsSpan());
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();
            return buffer.WrittenSpan.ToArray();
        }
        catch (Exception exception) when (exception is InvalidSemanticContract or InvalidOperationException or JsonException)
        {
            throw new InvalidScreenplayWorkspace("The workspace cannot be serialized canonically.", exception);
        }
    }

    /// <summary>
    /// Reads canonical version 1 JSON, validates its supplied catalog and revision, and recompiles its exact source.
    /// </summary>
    /// <param name="json">The complete canonical UTF-8 envelope. Callers must impose byte and resource limits before admission.</param>
    /// <returns>The verified workspace, including unsuccessful compilation and diagnostics for invalid-but-editable source.</returns>
    /// <exception cref="InvalidScreenplayWorkspace">
    /// The envelope is malformed, non-canonical, unsupported, duplicated, structurally inconsistent, or has a revision mismatch.
    /// A catalog that workspace admission would change is rejected rather than silently materialized or replaced.
    /// </exception>
    public static ScreenplayWorkspace Deserialize(ReadOnlySpan<byte> json)
    {
        try
        {
            var reader = new Utf8JsonReader(json, CanonicalJson.ReaderOptions);
            CatalogRead.RequiredToken(ref reader, JsonTokenType.StartObject, "workspace root");
            var seen = CatalogRead.NewSeen();
            string? schema = null;
            uint? version = null;
            ApplicationIdentity application = default;
            string? name = null;
            WorkspaceRevision revision = default;
            SemanticIdentityCatalog? catalog = null;
            ImmutableArray<WorkspaceDocument> documents = default;
            while (CatalogRead.NextProperty(ref reader, seen, "workspace root") is { } property)
            {
                switch (property)
                {
                    case "schema": schema = CatalogRead.String(ref reader, property); break;
                    case "schemaVersion": version = CatalogRead.UInt32(ref reader, property); break;
                    case "applicationIdentity": application = ApplicationIdentity.Parse(CatalogRead.String(ref reader, property)); break;
                    case "applicationName": name = CatalogRead.String(ref reader, property); break;
                    case "revision": revision = WorkspaceRevision.Parse(CatalogRead.String(ref reader, property)); break;
                    case "identityCatalog": catalog = ReadCatalog(ref reader, json); break;
                    case "documents": documents = CatalogRead.Array(ref reader, ReadDocument, property); break;
                    default: throw CatalogRead.Unknown(property, "workspace root");
                }
            }

            if (schema != Schema || version != SchemaVersion || !application.IsSet || name is null ||
                !revision.IsSet || catalog is null || documents.IsDefault)
            {
                throw new InvalidScreenplayWorkspace("The workspace root is missing a required field or uses an unsupported schema.");
            }

            if (reader.Read() || reader.BytesConsumed != json.Length)
            {
                throw new InvalidScreenplayWorkspace("Workspace JSON contains trailing data.");
            }

            var workspace = ScreenplayWorkspace.Create(application, name, documents, catalog);
            if (!SemanticIdentityCatalogSerializer.Serialize(catalog).AsSpan().SequenceEqual(
                SemanticIdentityCatalogSerializer.Serialize(workspace.IdentityCatalog)))
            {
                throw new InvalidScreenplayWorkspace("Workspace admission would change the supplied authoritative identity catalog.");
            }

            if (workspace.Revision != revision)
            {
                throw new InvalidScreenplayWorkspace("The supplied workspace revision does not match its exact content.");
            }

            if (!json.SequenceEqual(Serialize(workspace)))
            {
                throw new InvalidScreenplayWorkspace("Workspace JSON is valid but not canonical.");
            }

            return workspace;
        }
        catch (Exception exception) when (exception is InvalidSemanticContract or InvalidWorkspaceDocument or
            InvalidPortablePlayPath or JsonException or InvalidOperationException or FormatException)
        {
            throw new InvalidScreenplayWorkspace("The workspace transport is malformed or inconsistent.", exception);
        }
    }

    static SemanticIdentityCatalog ReadCatalog(ref Utf8JsonReader reader, ReadOnlySpan<byte> json)
    {
        CatalogRead.RequiredToken(ref reader, JsonTokenType.StartObject, "identityCatalog");
        var start = checked((int)reader.TokenStartIndex);
        reader.Skip();
        return SemanticIdentityCatalogSerializer.Deserialize(json[start..checked((int)reader.BytesConsumed)]);
    }

    static WorkspaceDocument ReadDocument(ref Utf8JsonReader reader)
    {
        CatalogRead.Object(ref reader, "workspace document");
        var seen = CatalogRead.NewSeen();
        DocumentId id = default;
        string? key = null;
        PortablePlayPath? path = null;
        byte[]? bytes = null;
        while (CatalogRead.NextProperty(ref reader, seen, "workspace document") is { } property)
        {
            switch (property)
            {
                case "id": id = DocumentId.Parse(CatalogRead.String(ref reader, property)); break;
                case "stableKey": key = CatalogRead.String(ref reader, property); break;
                case "path": path = PortablePlayPath.Parse(CatalogRead.String(ref reader, property)); break;
                case "bytes": bytes = Convert.FromBase64String(CatalogRead.String(ref reader, property)); break;
                default: throw CatalogRead.Unknown(property, "workspace document");
            }
        }

        CatalogRead.Required(id.IsSet && key is not null && path is not null && bytes is not null, "workspace document");
        return WorkspaceDocument.Create(id, key!, path!, bytes!);
    }
}

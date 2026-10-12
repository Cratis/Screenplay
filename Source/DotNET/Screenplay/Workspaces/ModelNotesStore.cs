// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Reads and canonically serializes the committed, nonsemantic notes sidecar.
/// Writes are owned by the MCP recovery journal, not this store.
/// </summary>
public static class ModelNotesStore
{
    /// <summary>
    /// The root-local sidecar filename.
    /// </summary>
    public const string FileName = "notes.json";

    /// <summary>
    /// The fixed absent-file revision, distinct from an empty document.
    /// </summary>
    public const string AbsentRevision = "nrev1:absent";

    /// <summary>
    /// The bounded UTF-8 sidecar size.
    /// </summary>
    public const int MaximumBytes = 4 * 1024 * 1024;

    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
#if NET9_0_OR_GREATER
        RespectRequiredConstructorParameters = true
#endif
    };

    /// <summary>
    /// Allocates a content-independent cryptographically random identity.
    /// </summary>
    /// <returns>A note_ identity with a 22-character URL-safe token.</returns>
    public static string NewId() => $"note_{Convert.ToBase64String(RandomNumberGenerator.GetBytes(16)).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";

    /// <summary>
    /// Reads a bounded file without turning unreadable notes into a model failure.
    /// </summary>
    /// <param name="root">The folder owning .screenplay.</param>
    /// <param name="application">The expected application identity.</param>
    /// <returns>The explicit readable, absent or unreadable result.</returns>
    public static ModelNotesRead Read(string root, ApplicationIdentity application)
    {
        var path = Path.Combine(root, ".screenplay", FileName);
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return ReadBytes(null, application);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaximumBytes) return new("unreadable", null, null, "Notes exceed the bounded file size.");
            var bytes = new byte[checked((int)stream.Length)];
            stream.ReadExactly(bytes);
            if (stream.ReadByte() != -1) return new("unreadable", null, null, "Notes changed during read.");
            return ReadBytes(bytes, application);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new("unreadable", null, null, exception.Message);
        }
    }

    /// <summary>
    /// Validates sidecar bytes without rewriting them. Noncanonical whitespace is accepted.
    /// </summary>
    /// <param name="bytes">The file bytes, or null for absence.</param>
    /// <param name="application">The expected application identity.</param>
    /// <returns>The explicit result; its revision hashes canonical bytes.</returns>
    public static ModelNotesRead ReadBytes(byte[]? bytes, ApplicationIdentity application)
    {
        if (bytes is null) return new("absent", AbsentRevision, new(application.ToString(), []));
        try
        {
            if (bytes.Length > MaximumBytes) throw new InvalidModelNotes("Notes exceed the bounded file size.");
            using var json = JsonDocument.Parse(bytes);
            ModelNotesValidation.UniqueProperties(json.RootElement);
            var envelope = json.RootElement.Deserialize<Envelope>(Options) ?? throw new InvalidModelNotes("Empty notes document.");
            if (!MatchesApplication(envelope, application))
            {
                throw new InvalidModelNotes("Unsupported notes schema/version or another application's identity.");
            }

            var document = new ModelNotesDocument(envelope.ApplicationIdentity, envelope.Notes);
            var canonical = Serialize(document);
            return new("ok", Revision(canonical), document);
        }
        catch (Exception exception) when (exception is JsonException or InvalidModelNotes or InvalidSemanticContract or InvalidOperationException or NullReferenceException)
        {
            return new("unreadable", null, null, exception.Message);
        }
    }

    /// <summary>
    /// Writes stable header keys and one sorted note per LF-terminated line.
    /// </summary>
    /// <param name="document">The validated document.</param>
    /// <returns>Canonical UTF-8 bytes with a trailing newline.</returns>
    /// <exception cref="InvalidModelNotes">The notes are invalid or exceed the bound.</exception>
    public static byte[] Serialize(ModelNotesDocument document)
    {
        ModelNotesValidation.Document(document);
        var lines = document.Notes.OrderBy(note => note.Id, StringComparer.Ordinal).Select(NoteJson);
        var contents = $"{{\n\"schema\":\"cratis.screenplay.notes\",\n\"schemaVersion\":1,\n\"applicationIdentity\":{JsonSerializer.Serialize(document.ApplicationIdentity)},\n\"notes\":[\n{string.Join(",\n", lines)}\n]\n}}\n";
        var bytes = Encoding.UTF8.GetBytes(contents);
        if (bytes.Length > MaximumBytes) throw new InvalidModelNotes("Notes exceed the bounded file size.");
        return bytes;
    }

    /// <summary>
    /// Hashes canonical bytes independently of source and identity revisions.
    /// </summary>
    /// <param name="canonical">Canonical notes bytes, or null for absence.</param>
    /// <returns>The nrev1 revision.</returns>
    public static string Revision(byte[]? canonical) => canonical is null ? AbsentRevision : $"nrev1:{Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant()}";

    static bool MatchesApplication(Envelope envelope, ApplicationIdentity application)
    {
        bool[] requirements = [envelope.Schema == "cratis.screenplay.notes", envelope.SchemaVersion == 1, envelope.ApplicationIdentity == application.ToString()];
        return requirements.All(requirement => requirement);
    }

    static string NoteJson(ModelNote note)
    {
        // Null resolution is required; optional anchor/ref members are omitted.
        var node = JsonSerializer.SerializeToNode(note, Options)!;
        if (note.Resolution is null) node["resolution"] = null;
        return node.ToJsonString(Options);
    }

    sealed record Envelope(string Schema, int SchemaVersion, string ApplicationIdentity, ImmutableArray<ModelNote> Notes);
}

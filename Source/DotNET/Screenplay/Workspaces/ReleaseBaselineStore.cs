// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>Reads and canonically serializes the nonsemantic release sidecar. The shared recovery journal owns writes.</summary>
public static class ReleaseBaselineStore
{
    /// <summary>The sidecar filename.</summary>
    public const string FileName = "release-baseline.json";

    /// <summary>The fixed absent-file revision.</summary>
    public const string AbsentRevision = "brev1:absent";

    /// <summary>The bounded sidecar size.</summary>
    public const int MaximumBytes = 4 * 1024 * 1024;

    internal static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new ReleaseBaselineComplianceJson() },
#if NET9_0_OR_GREATER
        RespectRequiredConstructorParameters = true
#endif
    };

    /// <summary>Reads without turning malformed release metadata into a model failure.</summary>
    /// <param name="root">The folder owning .screenplay.</param>
    /// <param name="application">The expected application identity.</param>
    /// <returns>The absent, valid or unreadable outcome.</returns>
    public static ReleaseBaselineRead Read(string root, ApplicationIdentity application)
    {
        var path = Path.Combine(root, ".screenplay", FileName);
        try
        {
            if (!File.Exists(path) && !Directory.Exists(path)) return ReadBytes(null, application);
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            if (stream.Length > MaximumBytes) return new("unreadable", null, null, "Release baseline exceeds the bounded file size.");
            var bytes = new byte[checked((int)stream.Length)];
            stream.ReadExactly(bytes);
            if (stream.ReadByte() != -1) return new("unreadable", null, null, "Release baseline changed during read.");
            return ReadBytes(bytes, application);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new("unreadable", null, null, exception.Message);
        }
    }

    /// <summary>Validates bytes, accepting noncanonical whitespace without rewriting.</summary>
    /// <param name="bytes">The bytes, or null for absence.</param>
    /// <param name="application">The expected application identity.</param>
    /// <returns>The outcome and independent canonical revision.</returns>
    public static ReleaseBaselineRead ReadBytes(byte[]? bytes, ApplicationIdentity application)
    {
        if (bytes is null) return new("absent", AbsentRevision, new(application.ToString(), []));
        try
        {
            if (bytes.Length > MaximumBytes) throw new InvalidReleaseBaseline("Release baseline exceeds the bounded file size.");
            using var json = JsonDocument.Parse(bytes);
            ModelNotesValidation.UniqueProperties(json.RootElement);
            RequireKeys(json.RootElement, ["schema", "schemaVersion", "applicationIdentity", "entries"]);
            foreach (var entry in json.RootElement.GetProperty("entries").EnumerateArray()) RequireEntry(entry);
            var envelope = json.RootElement.Deserialize<Envelope>(Options) ?? throw new InvalidReleaseBaseline("Empty release baseline.");
            if (!Supported(envelope, application))
            {
                throw new InvalidReleaseBaseline("Unsupported release baseline schema/version or another application's identity.");
            }

            var document = new ReleaseBaselineDocument(envelope.ApplicationIdentity, envelope.Entries);
            return new("valid", Revision(Serialize(document)), document);
        }
        catch (Exception exception) when (exception is JsonException or InvalidReleaseBaseline or InvalidModelNotes or InvalidSemanticContract or InvalidOperationException or NullReferenceException or KeyNotFoundException)
        {
            return new("unreadable", null, null, exception.Message);
        }
    }

    /// <summary>Writes stable header keys and one sorted entry per LF-terminated line.</summary>
    /// <param name="document">The baseline.</param>
    /// <returns>The canonical bytes.</returns>
    public static byte[] Serialize(ReleaseBaselineDocument document)
    {
        _ = ApplicationIdentity.Parse(document.ApplicationIdentity);
        if (!ValidEntries(document.Entries))
        {
            throw new InvalidReleaseBaseline("Release baseline requires at most 10000 unique event revisions.");
        }

        var entries = document.Entries.OrderBy(entry => entry.Event, StringComparer.Ordinal).ThenBy(entry => entry.Revision).Select(EntryJson);
        var contents = $"{{\n\"schema\":\"cratis.screenplay.release-baseline\",\n\"schemaVersion\":1,\n\"applicationIdentity\":{JsonSerializer.Serialize(document.ApplicationIdentity)},\n\"entries\":[\n{string.Join(",\n", entries)}\n]\n}}\n";
        var bytes = Encoding.UTF8.GetBytes(contents);
        if (bytes.Length > MaximumBytes) throw new InvalidReleaseBaseline("Release baseline exceeds the bounded file size.");
        return bytes;
    }

    /// <summary>Hashes canonical bytes independently of all model revisions.</summary>
    /// <param name="canonical">Canonical bytes, or null for absence.</param>
    /// <returns>The brev1 revision.</returns>
    public static string Revision(byte[]? canonical) => canonical is null ? AbsentRevision : $"brev1:{Convert.ToHexString(SHA256.HashData(canonical)).ToLowerInvariant()}";

    static bool Supported(Envelope envelope, ApplicationIdentity application)
    {
        if (envelope.Schema != "cratis.screenplay.release-baseline" || envelope.SchemaVersion != 1) return false;
        return envelope.ApplicationIdentity == application.ToString();
    }

    static bool ValidEntries(ImmutableArray<ReleaseBaselineEntry> entries)
    {
        if (entries.IsDefault || entries.Length > 10000) return false;
        return entries.Select(entry => (entry.Event, entry.Revision)).Distinct().Count() == entries.Length;
    }

    static string EntryJson(ReleaseBaselineEntry entry)
    {
        _ = EventContractId.Parse(entry.Event);
        if (entry.Revision == 0 || !UtcInstant(entry.AdmittedAt)) throw new InvalidReleaseBaseline("Invalid revision or UTC admission instant.");
        ModelNotesValidation.Text(entry.EventType);
        ModelNotesValidation.Text(entry.AdmittedIn);
        ValidateAddress(entry.LastAddress);
        var schema = ReleaseSchemaCanonical.Normalize(entry.Schema);
        if (schema.EventType != entry.EventType || ReleaseSchemaCanonical.Hash(schema) != entry.SchemaHash) throw new InvalidReleaseBaseline("Resolved schema hash or stored name mismatch.");
        return JsonSerializer.Serialize(entry with { Schema = schema }, Options);
    }

    static bool UtcInstant(DateTimeOffset value) => value != default && value.Offset == TimeSpan.Zero;

    static void ValidateAddress(ModelNotesAddress address)
    {
        if (address.Kind != "Event" || address.Parts.IsDefaultOrEmpty) throw new InvalidReleaseBaseline("Invalid event display address.");
        foreach (var part in address.Parts)
        {
            if (!Enum.TryParse<SemanticAddressPartKind>(part.Kind, out var kind) || !Enum.IsDefined(kind)) throw new InvalidReleaseBaseline("Invalid display address part.");
            ModelNotesValidation.Text(part.Key);
        }
    }

    static void RequireKeys(JsonElement value, string[] keys)
    {
        foreach (var key in keys)
        {
            if (!value.TryGetProperty(key, out _)) throw new InvalidReleaseBaseline($"Missing required baseline member '{key}'.");
        }
    }

    static void RequireEntry(JsonElement entry)
    {
        RequireKeys(entry, ["event", "revision", "eventType", "schemaHash", "schema", "lastAddress", "admittedIn", "admittedAt"]);
        RequireKeys(entry.GetProperty("lastAddress"), ["kind", "parts"]);
        foreach (var part in entry.GetProperty("lastAddress").GetProperty("parts").EnumerateArray()) RequireKeys(part, ["kind", "key"]);
        RequireKeys(entry.GetProperty("schema"), ["eventType", "properties"]);
        RequireProperties(entry.GetProperty("schema").GetProperty("properties"));
    }

    static void RequireProperties(JsonElement properties)
    {
        foreach (var property in properties.EnumerateArray())
        {
            RequireKeys(property, ["name", "optional", "collection", "type"]);
            RequireType(property.GetProperty("type"));
        }
    }

    static void RequireType(JsonElement type)
    {
        RequireKeys(type, ["kind"]);
        if (type.TryGetProperty("compliance", out var compliance) && compliance.TryGetProperty("personal", out var personal)) RequireKeys(personal, ["criminal"]);
        if (type.TryGetProperty("properties", out var nested)) RequireProperties(nested);
        if (type.TryGetProperty("members", out var members)) RequireMembers(members);
    }

    static void RequireMembers(JsonElement members)
    {
        foreach (var member in members.EnumerateArray()) RequireKeys(member, ["name", "number"]);
    }

    sealed record Envelope(string Schema, int SchemaVersion, string ApplicationIdentity, ImmutableArray<ReleaseBaselineEntry> Entries);
}

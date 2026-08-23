// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Provides strict canonical identity-catalog v1 serialization and reading.
/// </summary>
public static class SemanticIdentityCatalogSerializer
{
    const string Schema = "cratis.screenplay.semantic-identities";
    const uint SchemaVersion = 1;

    /// <summary>
    /// Serializes an identity catalog to canonical UTF-8 JSON without a BOM or whitespace.
    /// </summary>
    /// <param name="catalog">The catalog to serialize.</param>
    /// <returns>The canonical UTF-8 JSON bytes.</returns>
    /// <exception cref="InvalidSemanticContract">The catalog is malformed or ambiguous.</exception>
    public static byte[] Serialize(SemanticIdentityCatalog catalog)
    {
        if (catalog is null)
        {
            throw new InvalidSemanticContract("The semantic identity catalog cannot be null.");
        }

        var revision = ComputeRevision(catalog);
        if (catalog.Revision != revision)
        {
            throw new InvalidSemanticContract($"Identity catalog revision '{catalog.Revision}' does not match computed revision '{revision}'.");
        }

        return Write(catalog, revision);
    }

    /// <summary>
    /// Reads strict canonical identity-catalog v1 UTF-8 JSON and verifies its revision.
    /// </summary>
    /// <param name="json">The canonical UTF-8 JSON bytes.</param>
    /// <returns>The verified identity catalog.</returns>
    /// <exception cref="InvalidSemanticContract">The JSON is malformed, non-canonical, unknown, duplicated, ambiguous, or has a revision mismatch.</exception>
    public static SemanticIdentityCatalog Deserialize(ReadOnlySpan<byte> json)
    {
        if (json.IsEmpty)
        {
            throw new InvalidSemanticContract("Canonical identity catalog JSON cannot be empty.");
        }

        try
        {
            var reader = new Utf8JsonReader(json, CanonicalJson.ReaderOptions);
            CatalogRead.RequiredToken(ref reader, JsonTokenType.StartObject, "identity catalog root");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            string? schema = null;
            uint? schemaVersion = null;
            ApplicationIdentity application = default;
            CatalogRevision? revision = null;
            ImmutableArray<DocumentIdentityAssignment> documents = default;
            ImmutableArray<SemanticIdentityAssignment> semantics = default;
            ImmutableArray<EventContractIdentityAssignment> events = default;
            while (CatalogRead.NextProperty(ref reader, seen, "identity catalog root") is { } property)
            {
                switch (property)
                {
                    case "schema": schema = CatalogRead.String(ref reader, property); break;
                    case "schemaVersion": schemaVersion = CatalogRead.UInt32(ref reader, property); break;
                    case "applicationIdentity": application = ApplicationIdentity.Parse(CatalogRead.String(ref reader, property)); break;
                    case "revision": revision = CatalogRevision.Parse(CatalogRead.String(ref reader, property)); break;
                    case "documents": documents = CatalogRead.Array(ref reader, CatalogRead.DocumentAssignment, property); break;
                    case "semantics": semantics = CatalogRead.Array(ref reader, CatalogRead.SemanticAssignment, property); break;
                    case "eventContracts": events = CatalogRead.Array(ref reader, CatalogRead.EventAssignment, property); break;
                    default: throw CatalogRead.Unknown(property, "identity catalog root");
                }
            }

            if (schema != Schema || schemaVersion != SchemaVersion || !application.IsSet || revision is null ||
                documents.IsDefault || semantics.IsDefault || events.IsDefault)
            {
                throw new InvalidSemanticContract("The identity catalog root is missing a required field or uses an unsupported schema.");
            }

            if (reader.Read() || reader.BytesConsumed != json.Length)
            {
                throw new InvalidSemanticContract("Canonical identity catalog JSON contains trailing data.");
            }

            var catalog = SemanticIdentityCatalog.Create(application, documents, semantics, events);
            var expectedRevision = ComputeRevision(catalog);
            if (revision.Value != expectedRevision)
            {
                throw new InvalidSemanticContract($"Identity catalog revision '{revision}' does not match computed revision '{expectedRevision}'.");
            }

            var canonical = Serialize(catalog);
            if (!json.SequenceEqual(canonical))
            {
                throw new InvalidSemanticContract("Identity catalog JSON is valid but not canonical.");
            }

            return catalog;
        }
        catch (InvalidSemanticContract)
        {
            throw;
        }
        catch (JsonException error)
        {
            throw new InvalidSemanticContract($"Identity catalog JSON is malformed: {error.Message}");
        }
        catch (InvalidOperationException error)
        {
            throw new InvalidSemanticContract($"Identity catalog JSON is malformed: {error.Message}");
        }
    }

    internal static CatalogRevision ComputeRevision(SemanticIdentityCatalog catalog) => CatalogRevision.Compute(Write(catalog, null));

    static byte[] Write(SemanticIdentityCatalog catalog, CatalogRevision? revision)
    {
        try
        {
            var buffer = new ArrayBufferWriter<byte>();
            using var writer = new Utf8JsonWriter(buffer, CanonicalJson.WriterOptions);
            writer.WriteStartObject();
            writer.WriteString("schema", Schema);
            writer.WriteNumber("schemaVersion", SchemaVersion);
            writer.WriteString("applicationIdentity", catalog.Application.ToString());
            if (revision is not null)
            {
                writer.WriteString("revision", revision.Value.ToString());
            }

            WriteArray(writer, "documents", catalog.Documents.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteDocumentAssignment);
            WriteArray(writer, "semantics", catalog.Semantics.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteSemanticAssignment);
            WriteArray(writer, "eventContracts", catalog.EventContracts.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteEventAssignment);
            writer.WriteEndObject();
            writer.Flush();
            return buffer.WrittenSpan.ToArray();
        }
        catch (InvalidOperationException)
        {
            throw new InvalidSemanticContract($"The identity catalog exceeds the canonical maximum depth of {CanonicalJson.MaximumDepth}.");
        }
    }

    static void WriteDocumentAssignment(Utf8JsonWriter writer, DocumentIdentityAssignment assignment)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "key", assignment.Key);
        writer.WriteString("id", assignment.Id.ToString());
        writer.WriteString("origin", Origin(assignment.Origin));
        writer.WriteEndObject();
    }

    static void WriteSemanticAssignment(Utf8JsonWriter writer, SemanticIdentityAssignment assignment)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("address");
        WriteAddress(writer, assignment.Address);
        writer.WriteString("id", assignment.Id.ToString());
        writer.WriteString("origin", Origin(assignment.Origin));
        writer.WriteEndObject();
    }

    static void WriteEventAssignment(Utf8JsonWriter writer, EventContractIdentityAssignment assignment)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("address");
        WriteAddress(writer, assignment.Address);
        writer.WriteString("id", assignment.Id.ToString());
        writer.WriteNumber("contractRevision", assignment.Revision.Value);
        writer.WriteString("origin", Origin(assignment.Origin));
        writer.WriteEndObject();
    }

    static void WriteAddress(Utf8JsonWriter writer, SemanticAddress address)
    {
        writer.WriteStartObject();
        writer.WriteNumber("kind", (int)address.Kind);
        writer.WritePropertyName("parts");
        writer.WriteStartArray();
        foreach (var part in address.Parts)
        {
            writer.WriteStartObject();
            writer.WriteNumber("kind", (int)part.Kind);
            CanonicalJson.WriteString(writer, "key", part.Key);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    static void WriteArray<T>(Utf8JsonWriter writer, string name, IEnumerable<T> values, Action<Utf8JsonWriter, T> write)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values)
        {
            write(writer, value);
        }

        writer.WriteEndArray();
    }

    static string Origin(SemanticIdentityOrigin origin) => origin switch
    {
        SemanticIdentityOrigin.Persisted => "persisted",
        SemanticIdentityOrigin.LegacyBootstrap => "legacyBootstrap",
        _ => throw new InvalidSemanticContract($"Unknown semantic identity origin '{(int)origin}'.")
    };
}

static class CatalogRead
{
    internal delegate T ValueReader<T>(ref Utf8JsonReader reader);

    internal static DocumentIdentityAssignment DocumentAssignment(ref Utf8JsonReader reader)
    {
        Object(ref reader, "document assignment");
        var seen = NewSeen();
        string? key = null;
        DocumentId id = default;
        SemanticIdentityOrigin? origin = null;
        while (NextProperty(ref reader, seen, "document assignment") is { } property)
        {
            switch (property)
            {
                case "key": key = String(ref reader, property); break;
                case "id": id = DocumentId.Parse(String(ref reader, property)); break;
                case "origin": origin = ParseOrigin(String(ref reader, property)); break;
                default: throw Unknown(property, "document assignment");
            }
        }

        Required(key is not null && id.IsSet && origin is not null, "document assignment");
        return new(key!, id, origin!.Value);
    }

    internal static SemanticIdentityAssignment SemanticAssignment(ref Utf8JsonReader reader)
    {
        Object(ref reader, "semantic assignment");
        var seen = NewSeen();
        SemanticAddress? address = null;
        SemanticId id = default;
        SemanticIdentityOrigin? origin = null;
        while (NextProperty(ref reader, seen, "semantic assignment") is { } property)
        {
            switch (property)
            {
                case "address": RequiredToken(ref reader, JsonTokenType.StartObject, property); address = Address(ref reader); break;
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "origin": origin = ParseOrigin(String(ref reader, property)); break;
                default: throw Unknown(property, "semantic assignment");
            }
        }

        Required(address is not null && id.IsSet && origin is not null, "semantic assignment");
        return new(address!, id, origin!.Value);
    }

    internal static EventContractIdentityAssignment EventAssignment(ref Utf8JsonReader reader)
    {
        Object(ref reader, "event contract assignment");
        var seen = NewSeen();
        SemanticAddress? address = null;
        EventContractId id = default;
        EventContractRevision revision = default;
        SemanticIdentityOrigin? origin = null;
        while (NextProperty(ref reader, seen, "event contract assignment") is { } property)
        {
            switch (property)
            {
                case "address": RequiredToken(ref reader, JsonTokenType.StartObject, property); address = Address(ref reader); break;
                case "id": id = EventContractId.Parse(String(ref reader, property)); break;
                case "contractRevision": revision = new(UInt32(ref reader, property)); break;
                case "origin": origin = ParseOrigin(String(ref reader, property)); break;
                default: throw Unknown(property, "event contract assignment");
            }
        }

        Required(address is not null && id.IsSet && revision.IsValid && origin is not null, "event contract assignment");
        return new(address!, id, revision, origin!.Value);
    }

    internal static SemanticAddress Address(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticKind? kind = null;
        ImmutableArray<SemanticAddressPart> parts = default;
        while (NextProperty(ref reader, seen, "semantic address") is { } property)
        {
            switch (property)
            {
                case "kind": kind = ParseEnum<SemanticKind>(Int32(ref reader, property), "semantic kind"); break;
                case "parts": parts = Array(ref reader, AddressPart, property); break;
                default: throw Unknown(property, "semantic address");
            }
        }

        Required(kind is not null && !parts.IsDefault, "semantic address");
        return SemanticAddress.FromCanonical(kind!.Value, parts);
    }

    internal static SemanticAddressPart AddressPart(ref Utf8JsonReader reader)
    {
        Object(ref reader, "semantic address part");
        var seen = NewSeen();
        SemanticAddressPartKind? kind = null;
        string? key = null;
        while (NextProperty(ref reader, seen, "semantic address part") is { } property)
        {
            switch (property)
            {
                case "kind": kind = ParseEnum<SemanticAddressPartKind>(Int32(ref reader, property), "semantic address part kind"); break;
                case "key": key = String(ref reader, property); break;
                default: throw Unknown(property, "semantic address part");
            }
        }

        Required(kind is not null && key is not null, "semantic address part");
        return SemanticAddressPart.Create(kind!.Value, key!);
    }

    internal static ImmutableArray<T> Array<T>(ref Utf8JsonReader reader, ValueReader<T> read, string name)
    {
        RequiredToken(ref reader, JsonTokenType.StartArray, name);
        var values = ImmutableArray.CreateBuilder<T>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return values.ToImmutable();
            }

            values.Add(read(ref reader));
        }

        throw new InvalidSemanticContract($"The {name} array ended unexpectedly.");
    }

    internal static string? NextProperty(ref Utf8JsonReader reader, HashSet<string> seen, string owner)
    {
        if (!reader.Read())
        {
            throw new InvalidSemanticContract($"The {owner} object ended unexpectedly.");
        }

        if (reader.TokenType == JsonTokenType.EndObject)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.PropertyName)
        {
            throw Malformed(owner, "a property name");
        }

        var property = reader.GetString()!;
        if (!seen.Add(property))
        {
            throw new InvalidSemanticContract($"The {owner} contains duplicate property '{property}'.");
        }

        return property;
    }

    internal static void RequiredToken(ref Utf8JsonReader reader, JsonTokenType token, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType != token)
        {
            throw Malformed(name, token.ToString());
        }
    }

    internal static string String(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType != JsonTokenType.String)
        {
            throw Malformed(name, "a string");
        }

        return CanonicalJson.RequireNfc(reader.GetString()!, name);
    }

    internal static uint UInt32(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetUInt32(out var value))
        {
            throw Malformed(name, "an unsigned 32-bit integer");
        }

        return value;
    }

    internal static int Int32(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var value))
        {
            throw Malformed(name, "a signed 32-bit integer");
        }

        return value;
    }

    internal static T ParseEnum<T>(int value, string description)
        where T : struct, Enum
    {
        var parsed = (T)Enum.ToObject(typeof(T), value);
        if (!Enum.IsDefined(parsed) || value == -1)
        {
            throw new InvalidSemanticContract($"Unknown {description} '{value}'.");
        }

        return parsed;
    }

    internal static SemanticIdentityOrigin ParseOrigin(string value) => value switch
    {
        "persisted" => SemanticIdentityOrigin.Persisted,
        "legacyBootstrap" => SemanticIdentityOrigin.LegacyBootstrap,
        _ => throw new InvalidSemanticContract($"Unknown semantic identity origin '{value}'.")
    };

    internal static void Object(ref Utf8JsonReader reader, string name)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw Malformed(name, "an object");
        }
    }

    internal static void RequiredRead(ref Utf8JsonReader reader, string name)
    {
        if (!reader.Read())
        {
            throw new InvalidSemanticContract($"The {name} value is missing.");
        }
    }

    internal static void Required(bool condition, string owner)
    {
        if (!condition)
        {
            throw new InvalidSemanticContract($"The {owner} is missing one or more required fields.");
        }
    }

    internal static HashSet<string> NewSeen() => new(StringComparer.Ordinal);

    internal static InvalidSemanticContract Unknown(string property, string owner) =>
        new($"Unknown property '{property}' in {owner}.");

    internal static InvalidSemanticContract Malformed(string name, string expected) =>
        new($"The {name} value must be {expected}.");
}

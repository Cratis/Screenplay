// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal sealed class EsmCanonicalDocumentWriter(uint major, uint registeredMinor, bool projection, SemanticRevision? revision)
{
    internal byte[] Write(JsonElement root)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer, CanonicalJson.WriterOptions);
        WriteValue(writer, root, EsmBaselineShape.Envelope);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }

    static bool IsVersionProperty(string name) => name == "languageVersion" || name == "semanticVersion";

    static void WriteExtensions(Utf8JsonWriter writer, JsonElement value, ImmutableDictionary<string, EsmBaselineShape> fields)
    {
        foreach (var property in value.EnumerateObject().Where(property => !fields.ContainsKey(property.Name)).OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            writer.WritePropertyName(property.Name);
            EsmOpaqueJson.Write(writer, property.Value);
        }
    }

    void WriteValue(Utf8JsonWriter writer, JsonElement value, EsmBaselineShape shape)
    {
        if (shape == EsmBaselineShape.Atom)
        {
            // Baseline atoms retain their per-field encoding (for example the escaped
            // condition operator). The unchanged strict reader verifies these exact bytes.
            writer.WriteRawValue(value.GetRawText(), skipInputValidation: false);
            return;
        }

        switch (value.ValueKind)
        {
            case JsonValueKind.Array: WriteArray(writer, value, shape); break;
            case JsonValueKind.Object: WriteObject(writer, value, shape); break;
            default: writer.WriteRawValue(value.GetRawText(), skipInputValidation: false); break;
        }
    }

    void WriteArray(Utf8JsonWriter writer, JsonElement value, EsmBaselineShape shape)
    {
        writer.WriteStartArray();
        foreach (var element in value.EnumerateArray()) WriteValue(writer, element, shape);
        writer.WriteEndArray();
    }

    void WriteObject(Utf8JsonWriter writer, JsonElement value, EsmBaselineShape shape)
    {
        writer.WriteStartObject();
        var fields = EsmBaselineShapes.Fields(shape);
        foreach (var property in value.EnumerateObject().Where(property => fields.ContainsKey(property.Name)))
        {
            if (shape == EsmBaselineShape.Envelope) WriteEnvelopeProperty(writer, property);
            else WriteKnown(writer, value, shape, property);
        }

        if (!projection) WriteExtensions(writer, value, fields);
        writer.WriteEndObject();
    }

    void WriteKnown(Utf8JsonWriter writer, JsonElement parent, EsmBaselineShape shape, JsonProperty property)
    {
        writer.WritePropertyName(property.Name);
        WriteValue(writer, property.Value, EsmBaselineShapes.Child(shape, property.Name, parent));
    }

    void WriteEnvelopeProperty(Utf8JsonWriter writer, JsonProperty property)
    {
        if (property.Name == "revision")
        {
            if (revision is not null) writer.WriteString("revision", revision.Value.ToString());
            return;
        }

        if (projection && IsVersionProperty(property.Name))
        {
            writer.WriteString(property.Name, new SemanticVersion(major, registeredMinor).ToString());
            return;
        }

        writer.WritePropertyName(property.Name);
        WriteValue(writer, property.Value, EsmBaselineShapes.Fields(EsmBaselineShape.Envelope)[property.Name]);
    }
}

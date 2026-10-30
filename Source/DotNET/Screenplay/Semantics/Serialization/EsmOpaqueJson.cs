// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// The knowledge-independent opaque grammar from decision 0118: ordinal object names,
/// received array order, no omission/default normalization, canonical strings and decimals.
/// </summary>
internal static class EsmOpaqueJson
{
    internal static void Validate(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object: ValidateObject(value); break;
            case JsonValueKind.Array:
                foreach (var element in value.EnumerateArray()) Validate(element);
                break;
            case JsonValueKind.String: CanonicalJson.RequireNfc(value.GetString()!, "string value"); break;
        }
    }

    internal static void ValidateName(string name)
    {
        if (name.Length is 0 or > 64) throw InvalidName();
        if (!char.IsAsciiLetterLower(name[0]) || !name.All(char.IsAsciiLetterOrDigit)) throw InvalidName();
    }

    internal static void ValidateNames(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object: ValidateObjectNames(value); break;
            case JsonValueKind.Array:
                foreach (var element in value.EnumerateArray()) ValidateNames(element);
                break;
        }
    }

    internal static void Write(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object: WriteObject(writer, value); break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var element in value.EnumerateArray()) Write(writer, element);
                writer.WriteEndArray();
                break;
            default: WriteScalar(writer, value); break;
        }
    }

    static InvalidSemanticContract InvalidName() => new("An unknown ESM member name must be an ASCII schema token of at most 64 characters.");

    static void ValidateObject(JsonElement value)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
        {
            if (!seen.Add(property.Name)) throw new InvalidSemanticContract($"Duplicate ESM member '{property.Name}'.");
            CanonicalJson.RequireNfc(property.Name, "member name");
            Validate(property.Value);
        }
    }

    static void ValidateObjectNames(JsonElement value)
    {
        foreach (var property in value.EnumerateObject())
        {
            ValidateName(property.Name);
            ValidateNames(property.Value);
        }
    }

    static void WriteObject(Utf8JsonWriter writer, JsonElement value)
    {
        writer.WriteStartObject();
        foreach (var property in value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
        {
            writer.WritePropertyName(CanonicalJson.RequireNfc(property.Name, "member name"));
            Write(writer, property.Value);
        }

        writer.WriteEndObject();
    }

    static void WriteScalar(Utf8JsonWriter writer, JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String: CanonicalJson.WriteStringValue(writer, value.GetString()!); break;
            case JsonValueKind.Number: writer.WriteRawValue(CanonicalJson.DecimalText(value.GetDecimal()), skipInputValidation: false); break;
            case JsonValueKind.True: writer.WriteBooleanValue(true); break;
            case JsonValueKind.False: writer.WriteBooleanValue(false); break;
            case JsonValueKind.Null: writer.WriteNullValue(); break;
            default: throw new InvalidSemanticContract("The ESM contains an invalid JSON value.");
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
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
        return StringToken(ref reader, name, "a string");
    }

    internal static string? NullableString(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        return reader.TokenType == JsonTokenType.Null ? null : StringToken(ref reader, name, "a string or null");
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

    internal static bool Boolean(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        return BooleanToken(ref reader, name, "a Boolean");
    }

    internal static bool? NullableBoolean(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        return reader.TokenType == JsonTokenType.Null ? null : BooleanToken(ref reader, name, "a Boolean or null");
    }

    internal static decimal Decimal(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        return DecimalToken(ref reader, name, "a decimal number");
    }

    internal static decimal? NullableDecimal(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        return reader.TokenType == JsonTokenType.Null ? null : DecimalToken(ref reader, name, "a decimal number or null");
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

    internal static ImmutableArray<string> StringArray(ref Utf8JsonReader reader, string name)
    {
        RequiredToken(ref reader, JsonTokenType.StartArray, name);
        var values = ImmutableArray.CreateBuilder<string>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return values.ToImmutable();
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw Malformed(name, "an array of strings");
            }

            values.Add(reader.GetString()!);
        }

        throw new InvalidSemanticContract($"The {name} array ended unexpectedly.");
    }

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

    internal static InvalidSemanticContract DiscriminatorError(string value, string description) =>
        new($"Unknown {description} discriminator '{value}'.");

    static string StringToken(ref Utf8JsonReader reader, string name, string expected)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw Malformed(name, expected);
        }

        return CanonicalJson.RequireNfc(reader.GetString()!, name);
    }

    static bool BooleanToken(ref Utf8JsonReader reader, string name, string expected) => reader.TokenType switch
    {
        JsonTokenType.True => true,
        JsonTokenType.False => false,
        _ => throw Malformed(name, expected)
    };

    static decimal DecimalToken(ref Utf8JsonReader reader, string name, string expected)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetDecimal(out var value))
        {
            throw Malformed(name, expected);
        }

        return value;
    }
}
#pragma warning restore IDE0350

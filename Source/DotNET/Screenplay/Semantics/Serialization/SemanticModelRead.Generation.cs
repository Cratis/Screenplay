// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    static SemanticGeneration Generation(ref Utf8JsonReader reader)
    {
        Object(ref reader, "generation");
        var seen = NewSeen();
        string? kind = null, prefix = null, letterCase = null, alphabet = null, encoding = null;
        int? size = null, count = null;
        string? pattern = null, checksum = null, requirement = null;
        long? minimum = null, maximum = null;
        while (NextProperty(ref reader, seen, "generation") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "pattern": pattern = String(ref reader, property); break;
                case "checksum": checksum = String(ref reader, property); break;
                case "requirement": requirement = String(ref reader, property); break;
                case "min": minimum = GenerationInt64(ref reader, property); break;
                case "max": maximum = GenerationInt64(ref reader, property); break;
                case "prefix": prefix = String(ref reader, property); break;
                case "case": letterCase = String(ref reader, property); break;
                case "alphabet": alphabet = String(ref reader, property); break;
                case "encoding": encoding = String(ref reader, property); break;
                case "size": size = GenerationInteger(ref reader, property); break;
                case "count": count = GenerationInteger(ref reader, property); break;
                default: throw Unknown(property, "generation");
            }
        }

        Required(kind is not null, "generation");
        return new(kind!) { Prefix = prefix, Case = letterCase, Size = size, Alphabet = alphabet, Count = count, Encoding = encoding, Pattern = pattern, Checksum = checksum, Minimum = minimum, Maximum = maximum, RequirementId = requirement };
    }

    static long GenerationInt64(ref Utf8JsonReader reader, string property)
    {
        RequiredRead(ref reader, property);
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out var value)) throw Malformed(property, "an integral Int64 endpoint");

        return value;
    }

    static int GenerationInteger(ref Utf8JsonReader reader, string property)
    {
        RequiredRead(ref reader, property);
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var value))
        {
            throw Malformed(property, "a 32-bit integer");
        }

        return value;
    }
}

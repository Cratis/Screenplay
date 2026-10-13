// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    static void WriteGeneration(Utf8JsonWriter writer, SemanticGeneration generation)
    {
        writer.WriteStartObject("generation");
        writer.WriteString("kind", generation.Kind);
        if (generation.Size is { } size) writer.WriteNumber("size", size);
        if (generation.Count is { } count) writer.WriteNumber("count", count);
        if (generation.Encoding is { } encoding) writer.WriteString("encoding", encoding);
        if (generation.Alphabet is { } alphabet) CanonicalJson.WriteString(writer, "alphabet", alphabet);
        if (generation.Prefix is { } prefix) CanonicalJson.WriteString(writer, "prefix", prefix);
        if (generation.Case is { } letterCase) writer.WriteString("case", letterCase);
        writer.WriteEndObject();
    }
}

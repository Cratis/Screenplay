// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Tool;

// The JSON form of the diff report: the library ModelDifference, camelCase, enums and identifiers as strings.
static class DiffJson
{
    static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
            new IdentifierJson<SemanticId>(),
            new IdentifierJson<EventContractId>(),
            new IdentifierJson<DocumentId>()
        }
    };

    internal static void Write(DiffReport report, TextWriter output) => output.WriteLine(JsonSerializer.Serialize(report, _options));

    sealed class IdentifierJson<T> : JsonConverter<T>
        where T : struct
    {
        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            throw new NotSupportedException("The diff report is write-only.");

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString());
    }
}

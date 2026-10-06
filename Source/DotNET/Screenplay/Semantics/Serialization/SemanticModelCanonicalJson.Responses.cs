// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    static void WriteResponse(Utf8JsonWriter writer, SemanticCommandResponse response)
    {
        writer.WriteStartObject();
        switch (response)
        {
            case SemanticScalarCommandResponse scalar:
                writer.WriteString("kind", "scalar");
                writer.WriteString("source", scalar.Source.ToString());
                writer.WritePropertyName("type");
                WriteTypeReference(writer, scalar.Type);
                break;
            case SemanticRecordCommandResponse record:
                writer.WriteString("kind", "record");
                WriteArray(writer, "fields", record.Fields, (output, field) =>
                {
                    output.WriteStartObject();
                    CanonicalJson.WriteString(output, "name", field.Name);
                    output.WriteString("source", field.Source.ToString());
                    output.WritePropertyName("type");
                    WriteTypeReference(output, field.Type);
                    output.WriteEndObject();
                });
                break;
            default: throw new InvalidSemanticContract("Unknown command response variant.");
        }
        writer.WriteEndObject();
    }

    static void WriteThenReturns(Utf8JsonWriter writer, SemanticSpecificationResponse response)
    {
        writer.WriteStartObject();
        switch (response)
        {
            case SemanticScalarSpecificationResponse scalar:
                writer.WriteString("kind", "scalar");
                writer.WritePropertyName("value");
                WriteValue(writer, scalar.Value);
                break;
            case SemanticRecordSpecificationResponse record:
                writer.WriteString("kind", "record");
                WriteArray(writer, "fields", record.Fields.OrderBy(field => field.Name, StringComparer.Ordinal), (output, field) =>
                {
                    output.WriteStartObject();
                    CanonicalJson.WriteString(output, "name", field.Name);
                    output.WritePropertyName("value");
                    WriteValue(output, field.Value);
                    output.WriteEndObject();
                });
                break;
            default: throw new InvalidSemanticContract("Unknown return expectation variant.");
        }
        writer.WriteEndObject();
    }
}

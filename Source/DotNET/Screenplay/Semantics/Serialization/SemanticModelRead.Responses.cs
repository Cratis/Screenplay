// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    internal static SemanticCommandResponse Response(ref Utf8JsonReader reader)
    {
        Object(ref reader, "response");
        var seen = NewSeen();
        string? kind = null;
        SemanticId source = default;
        SemanticTypeReference? type = null;
        ImmutableArray<SemanticCommandResponseField> fields = default;
        while (NextProperty(ref reader, seen, "response") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "source": source = SemanticId.Parse(String(ref reader, property)); break;
                case "type": RequiredToken(ref reader, JsonTokenType.StartObject, property); type = TypeReference(ref reader); break;
                case "fields": fields = Array(ref reader, ResponseField, property); break;
                default: throw Unknown(property, "response");
            }
        }

        return kind switch
        {
            "scalar" when source.IsSet && type is not null && fields.IsDefault => new SemanticScalarCommandResponse(source, type),
            "record" when !source.IsSet && type is null && !fields.IsDefaultOrEmpty => new SemanticRecordCommandResponse(fields),
            _ => throw Malformed("response", "a scalar with source and type, or a non-empty record of fields")
        };
    }

    internal static SemanticCommandResponseField ResponseField(ref Utf8JsonReader reader)
    {
        Object(ref reader, "response field");
        var seen = NewSeen();
        string? name = null;
        SemanticId source = default;
        SemanticTypeReference? type = null;
        while (NextProperty(ref reader, seen, "response field") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "source": source = SemanticId.Parse(String(ref reader, property)); break;
                case "type": RequiredToken(ref reader, JsonTokenType.StartObject, property); type = TypeReference(ref reader); break;
                default: throw Unknown(property, "response field");
            }
        }

        Required(!string.IsNullOrWhiteSpace(name) && source.IsSet && type is not null, "response field");
        return new(name!, type!, source);
    }

    internal static SemanticSpecificationResponse ThenReturns(ref Utf8JsonReader reader)
    {
        Object(ref reader, "then returns");
        var seen = NewSeen();
        string? kind = null;
        SemanticValue? value = null;
        ImmutableArray<SemanticSpecificationResponseField> fields = default;
        while (NextProperty(ref reader, seen, "then returns") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, property); value = Value(ref reader); break;
                case "fields": fields = Array(ref reader, ReturnField, property); break;
                default: throw Unknown(property, "then returns");
            }
        }

        return kind switch
        {
            "scalar" when value is not null && fields.IsDefault => new SemanticScalarSpecificationResponse(value),
            "record" when value is null && !fields.IsDefaultOrEmpty => new SemanticRecordSpecificationResponse(fields),
            _ => throw Malformed("then returns", "a scalar value or a non-empty record of fields")
        };
    }

    internal static SemanticSpecificationResponseField ReturnField(ref Utf8JsonReader reader)
    {
        Object(ref reader, "return field");
        var seen = NewSeen();
        string? name = null;
        SemanticValue? value = null;
        while (NextProperty(ref reader, seen, "return field") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, property); value = Value(ref reader); break;
                default: throw Unknown(property, "return field");
            }
        }

        Required(!string.IsNullOrWhiteSpace(name) && value is not null, "return field");
        return new(name!, value!);
    }
}

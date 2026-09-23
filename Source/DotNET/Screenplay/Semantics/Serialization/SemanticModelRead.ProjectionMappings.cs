// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Reads projection mappings and values strictly.
/// </summary>
internal static partial class SemanticModelRead
{
    static SemanticProjectionMapping ProjectionMapping(ref Utf8JsonReader reader)
    {
        Object(ref reader, "projection mapping");
        var seen = NewSeen();
        ImmutableArray<SemanticId> target = default;
        SemanticProjectionOperation? operation = null;
        SemanticProjectionValue? source = null;
        var sourceRead = false;
        while (NextProperty(ref reader, seen, "projection mapping") is { } property)
        {
            switch (property)
            {
                case "target": target = IdArray(ref reader, property); break;
                case "operation": operation = ParseProjectionOperation(String(ref reader, property)); break;
                case "source": sourceRead = true; source = NullableObject(ref reader, property, ProjectionValue); break;
                default: throw Unknown(property, "projection mapping");
            }
        }

        Required(!target.IsDefault && operation is not null && sourceRead, "projection mapping");
        return new(target, operation!.Value, source);
    }

    static SemanticProjectionValue ProjectionValue(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        string? kind = null;
        SemanticValue? value = null;
        ImmutableArray<SemanticId> path = default;
        string? contextPath = null;
        while (NextProperty(ref reader, seen, "projection value") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, property); value = Value(ref reader); break;
                case "path" when kind == "eventProperty": path = IdArray(ref reader, property); break;
                case "path" when kind == "eventContext": contextPath = String(ref reader, property); break;
                case "path": throw Malformed("projection value", "a kind discriminator before its path");
                default: throw Unknown(property, "projection value");
            }
        }

        return kind switch
        {
            "literal" when value is not null && path.IsDefault && contextPath is null => new SemanticProjectionLiteral(value),
            "eventProperty" when value is null && !path.IsDefault && contextPath is null => new SemanticProjectionEventProperty(path),
            "eventSourceIdentity" when value is null && path.IsDefault && contextPath is null => SemanticProjectionValue.EventSourceIdentity,
            "eventContext" when value is null && path.IsDefault && contextPath is not null => new SemanticProjectionEventContextValue(contextPath),
            not null and not ("literal" or "eventProperty" or "eventSourceIdentity" or "eventContext") => throw DiscriminatorError(kind, "projection value kind"),
            _ => throw Malformed("projection value", "one exact projection value variant")
        };
    }

    static T? NullableObject<T>(ref Utf8JsonReader reader, string name, ValueReader<T> read)
        where T : class
    {
        RequiredRead(ref reader, name);
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.StartObject => read(ref reader),
            _ => throw Malformed(name, "an object or null")
        };
    }

    static ImmutableArray<SemanticId> IdArray(ref Utf8JsonReader reader, string name) =>
        [.. StringArray(ref reader, name).Select(SemanticId.Parse)];

    static SemanticProjectionOperation ParseProjectionOperation(string value) => value switch
    {
        "set" => SemanticProjectionOperation.Set,
        "clear" => SemanticProjectionOperation.Clear,
        "add" => SemanticProjectionOperation.Add,
        "subtract" => SemanticProjectionOperation.Subtract,
        "increment" => SemanticProjectionOperation.Increment,
        "decrement" => SemanticProjectionOperation.Decrement,
        _ => throw DiscriminatorError(value, "projection operation")
    };
}

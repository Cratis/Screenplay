// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep parsing delegates clear.
internal static partial class SemanticModelRead
{
    static SemanticEventSource EventSourceDeclaration(ref Utf8JsonReader reader)
    {
        Object(ref reader, "event source declaration");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        string? sourceKind = null;
        SemanticTypeReference? identifier = null;
        ImmutableArray<SemanticEventStream> streams = default;
        while (NextProperty(ref reader, seen, "event source declaration") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "sourceKind": sourceKind = String(ref reader, property); break;
                case "identifierType": RequiredToken(ref reader, JsonTokenType.StartObject, property); identifier = TypeReference(ref reader); break;
                case "streams": streams = Array(ref reader, EventStream, property); break;
                default: throw Unknown(property, "event source declaration");
            }
        }
        Required(id.IsSet && name is not null && sourceKind is not null && !streams.IsDefault, "event source declaration");

        return new(id, name!, sourceKind!, streams) { IdentifierType = identifier };
    }

    static SemanticEventStream EventStream(ref Utf8JsonReader reader)
    {
        Object(ref reader, "event stream");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        string? streamKind = null;
        SemanticTypeReference? type = null;
        ImmutableArray<SemanticStreamIdPart> parts = [];
        while (NextProperty(ref reader, seen, "event stream") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "streamKind": streamKind = String(ref reader, property); break;
                case "streamIdType": RequiredToken(ref reader, JsonTokenType.StartObject, property); type = TypeReference(ref reader); break;
                case "streamIdParts":
                    parts = Array(ref reader, StreamIdPart, property);
                    Required(parts.Length >= 2, "composite stream identity declaration");
                    break;
                default: throw Unknown(property, "event stream");
            }
        }
        Required(id.IsSet && name is not null && streamKind is not null && (type is null || parts.IsEmpty), "event stream");

        return new(id, name!, streamKind!) { StreamIdType = type, StreamIdParts = parts };
    }

    static SemanticStreamIdPart StreamIdPart(ref Utf8JsonReader reader)
    {
        Object(ref reader, "stream identity part");
        var seen = NewSeen();
        string? name = null;
        SemanticTypeReference? type = null;
        while (NextProperty(ref reader, seen, "stream identity part") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "type": RequiredToken(ref reader, JsonTokenType.StartObject, property); type = TypeReference(ref reader); break;
                default: throw Unknown(property, "stream identity part");
            }
        }
        Required(name is not null && type is not null, "stream identity part");

        return new(name!, type!);
    }

    static SemanticObserverFilter ObserverFilter(ref Utf8JsonReader reader)
    {
        Object(ref reader, "observer filter");
        var seen = NewSeen();
        SemanticId source = default;
        SemanticId? stream = null;
        while (NextProperty(ref reader, seen, "observer filter") is { } property)
        {
            switch (property)
            {
                case "source": source = SemanticId.Parse(String(ref reader, property)); break;
                case "stream": stream = SemanticId.Parse(String(ref reader, property)); break;
                default: throw Unknown(property, "observer filter");
            }
        }
        Required(source.IsSet && stream?.IsSet != false, "observer filter");

        return new(source, stream);
    }

    static SemanticCommandRoute CommandRoute(ref Utf8JsonReader reader)
    {
        var (source, stream, scalar, parts) = Route(ref reader, Expression);

        return new(source, stream) { StreamId = scalar, StreamIdParts = [.. parts.Select(part => new SemanticCommandRoutePart(part.Part, part.Value))] };
    }

    static SemanticFixtureRoute FixtureRoute(ref Utf8JsonReader reader)
    {
        var (source, stream, scalar, parts) = Route(ref reader, Value);

        return new(source, stream) { StreamId = scalar, StreamIdParts = [.. parts.Select(part => new SemanticFixtureRoutePart(part.Part, part.Value))] };
    }

    static (SemanticId Source, SemanticId Stream, T? Scalar, ImmutableArray<(string Part, T Value)> Parts) Route<T>(ref Utf8JsonReader reader, ValueReader<T> readValue)
        where T : class
    {
        Object(ref reader, "event route");
        var seen = NewSeen();
        SemanticId source = default;
        SemanticId stream = default;
        T? scalar = null;
        ImmutableArray<(string Part, T Value)> parts = [];
        while (NextProperty(ref reader, seen, "event route") is { } property)
        {
            switch (property)
            {
                case "source": source = SemanticId.Parse(String(ref reader, property)); break;
                case "stream": stream = SemanticId.Parse(String(ref reader, property)); break;
                case "streamId": RequiredToken(ref reader, JsonTokenType.StartObject, property); scalar = readValue(ref reader); break;
                case "streamIdParts":
                    parts = Array(ref reader, (ref Utf8JsonReader item) => RoutePart(ref item, readValue), property);
                    Required(parts.Length >= 2, "composite stream identity mapping");
                    break;
                default: throw Unknown(property, "event route");
            }
        }
        Required(source.IsSet && stream.IsSet && (scalar is null || parts.IsEmpty), "event route");

        return (source, stream, scalar, parts);
    }

    static (string Part, T Value) RoutePart<T>(ref Utf8JsonReader reader, ValueReader<T> readValue)
        where T : class
    {
        Object(ref reader, "stream identity mapping");
        var seen = NewSeen();
        string? part = null;
        T? value = null;
        while (NextProperty(ref reader, seen, "stream identity mapping") is { } property)
        {
            switch (property)
            {
                case "part": part = String(ref reader, property); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, property); value = readValue(ref reader); break;
                default: throw Unknown(property, "stream identity mapping");
            }
        }
        Required(part is not null && value is not null, "stream identity mapping");

        return (part!, value!);
    }
}

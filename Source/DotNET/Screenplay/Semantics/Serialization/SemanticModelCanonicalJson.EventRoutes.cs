// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    static void WriteEventSourceDeclaration(Utf8JsonWriter writer, SemanticEventSource source)
    {
        writer.WriteStartObject();
        WriteId(writer, source.Id);
        CanonicalJson.WriteString(writer, "name", source.Name);
        CanonicalJson.WriteString(writer, "sourceKind", source.SourceKind);
        if (source.IdentifierType is { } type)
        {
            writer.WritePropertyName("identifierType");
            WriteTypeReference(writer, type);
        }
        WriteArray(writer, "streams", source.Streams.OrderBy(stream => stream.Id.ToString(), StringComparer.Ordinal), WriteEventStream);
        writer.WriteEndObject();
    }

    static void WriteEventStream(Utf8JsonWriter writer, SemanticEventStream stream)
    {
        writer.WriteStartObject();
        WriteId(writer, stream.Id);
        CanonicalJson.WriteString(writer, "name", stream.Name);
        CanonicalJson.WriteString(writer, "streamKind", stream.StreamKind);
        if (stream.StreamIdType is { } type)
        {
            writer.WritePropertyName("streamIdType");
            WriteTypeReference(writer, type);
        }
        if (!stream.StreamIdParts.IsEmpty)
        {
            WriteArray(writer, "streamIdParts", stream.StreamIdParts, (output, part) =>
            {
                output.WriteStartObject();
                CanonicalJson.WriteString(output, "name", part.Name);
                output.WritePropertyName("type");
                WriteTypeReference(output, part.Type);
                output.WriteEndObject();
            });
        }
        writer.WriteEndObject();
    }

    static void WriteObserverFilter(Utf8JsonWriter writer, SemanticObserverFilter filter)
    {
        writer.WritePropertyName("from");
        writer.WriteStartObject();
        writer.WriteString("source", filter.Source.ToString());
        if (filter.Stream is { } stream) writer.WriteString("stream", stream.ToString());
        writer.WriteEndObject();
    }

    static void WriteCommandRoute(Utf8JsonWriter writer, SemanticCommandRoute route)
    {
        WriteRouteStart(writer, route.Source, route.Stream);
        if (route.StreamId is { } expression)
        {
            writer.WritePropertyName("streamId");
            WriteExpression(writer, expression);
        }
        if (!route.StreamIdParts.IsEmpty)
        {
            WriteArray(writer, "streamIdParts", route.StreamIdParts, (output, part) =>
            {
                output.WriteStartObject();
                CanonicalJson.WriteString(output, "part", part.Part);
                output.WritePropertyName("value");
                WriteExpression(output, part.Value);
                output.WriteEndObject();
            });
        }
        writer.WriteEndObject();
    }

    static void WriteFixtureRoute(Utf8JsonWriter writer, SemanticFixtureRoute route)
    {
        WriteRouteStart(writer, route.Source, route.Stream);
        if (route.StreamId is { } value)
        {
            writer.WritePropertyName("streamId");
            WriteValue(writer, value);
        }
        if (!route.StreamIdParts.IsEmpty)
        {
            WriteArray(writer, "streamIdParts", route.StreamIdParts, (output, part) =>
            {
                output.WriteStartObject();
                CanonicalJson.WriteString(output, "part", part.Part);
                output.WritePropertyName("value");
                WriteValue(output, part.Value);
                output.WriteEndObject();
            });
        }
        writer.WriteEndObject();
    }

    static void WriteRouteStart(Utf8JsonWriter writer, SemanticId source, SemanticId stream)
    {
        writer.WritePropertyName("route");
        writer.WriteStartObject();
        writer.WriteString("source", source.ToString());
        writer.WriteString("stream", stream.ToString());
    }
}

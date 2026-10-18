// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Observations;

public static partial class ObservationReportJson
{
    static void WriteSlice(Utf8JsonWriter writer, SliceObservation slice)
    {
        writer.WriteStartObject();
        writer.WriteString("slice", slice.Slice.ToString());
        Address(writer, slice.Address);
        writer.WriteString("kind", slice.Kind.ToString());
        writer.WriteStartArray("events");
        foreach (var value in slice.Events)
        {
            writer.WriteStartObject();
            writer.WriteString("event", value.Event.ToString());
            Text(writer, "typeId", value.TypeId);
            writer.WriteStartArray("generationCounts");
            foreach (var append in value.GenerationCounts)
            {
                writer.WriteStartObject();
                writer.WriteStartObject("type");
                writer.WriteString("typeId", append.Type.TypeId);
                writer.WriteNumber("generation", append.Type.Generation);
                writer.WriteEndObject();
                writer.WriteNumber("count", append.Count);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteBoolean("registered", value.Registered);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteStartArray("producers");
        foreach (var value in slice.Producers) WriteProducer(writer, value);
        writer.WriteEndArray();
        writer.WriteStartArray("consumers");
        foreach (var value in slice.Consumers) WriteConsumer(writer, value);
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    static void WriteProducer(Utf8JsonWriter writer, SliceProducerObservation producer)
    {
        writer.WriteStartObject();
        writer.WriteString("construct", producer.Construct.ToString());
        writer.WriteString("kind", producer.Kind.ToString());
        writer.WriteStartArray("edges");
        foreach (var edge in producer.Edges)
        {
            writer.WriteStartObject();
            writer.WriteString("event", edge.Event.ToString());
            writer.WriteNumber("attributedEvents", edge.AttributedEvents);
            writer.WriteString("outcome", edge.Outcome.ToString());
            Text(writer, "reason", edge.Reason?.ToString());
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    static void WriteConsumer(Utf8JsonWriter writer, SliceConsumerObservation consumer)
    {
        writer.WriteStartObject();
        writer.WriteString("construct", consumer.Construct.ToString());
        writer.WriteString("event", consumer.Event.ToString());
        writer.WriteStartArray("references");
        foreach (var reference in consumer.References)
        {
            writer.WriteStartObject();
            writer.WriteString("identifier", reference.Identifier);
            writer.WriteString("sequenceId", reference.SequenceId);
            Number(writer, "nextSequenceNumber", reference.NextSequenceNumber);
            Number(writer, "lastHandledSequenceNumber", reference.LastHandledSequenceNumber);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteString("outcome", consumer.Outcome.ToString());
        Text(writer, "reason", consumer.Reason?.ToString());
        writer.WriteEndObject();
    }
}

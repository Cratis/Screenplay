// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Observations;

public static partial class ObservationReportJson
{
    static void WriteCoverage(Utf8JsonWriter writer, ObservationCoverage coverage)
    {
        writer.WriteStartObject("coverage");
        writer.WriteString("eventStore", coverage.EventStore);
        writer.WriteString("namespace", coverage.Namespace);
        writer.WriteStartObject("sequence");
        var sequence = coverage.Sequence;
        writer.WriteString("sequenceId", sequence.SequenceId);
        writer.WriteBoolean("startsAtFirstEvent", sequence.StartsAtFirstEvent);
        Number(writer, "firstExamined", sequence.FirstExamined);
        Number(writer, "lastExamined", sequence.LastExamined);
        Number(writer, "capturedTail", sequence.CapturedTail);
        writer.WriteStartArray("unexamined");
        foreach (var range in sequence.Unexamined)
        {
            writer.WriteStartObject();
            writer.WriteNumber("from", range.From);
            writer.WriteNumber("to", range.To);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        if (sequence.OccurredFrom is { } from) writer.WriteString("occurredFrom", from);
        if (sequence.OccurredTo is { } to) writer.WriteString("occurredTo", to);
        writer.WriteBoolean("complete", sequence.Complete);
        writer.WriteEndObject();
        writer.WriteString("capturedAt", coverage.CapturedAt);
        writer.WriteNumber("appendedEvents", coverage.AppendedEvents);
        writer.WriteNumber("attributedEvents", coverage.AttributedEvents);
        writer.WriteNumber("unattributedEvents", coverage.UnattributedEvents);
        writer.WriteEndObject();
    }
}

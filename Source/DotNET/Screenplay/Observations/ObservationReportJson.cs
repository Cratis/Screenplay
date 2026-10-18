// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Observations;

/// <summary>
/// Writes deterministic result JSON with fixed field order, camelCase names, string enums and omitted nulls.
/// </summary>
public static partial class ObservationReportJson
{
    /// <summary>
    /// Serializes the same report consumed by human views. No snapshot transport format is implied.
    /// </summary>
    /// <param name="report">The comparison result.</param>
    /// <returns>The complete compact UTF-8 result JSON.</returns>
    public static byte[] Serialize(ObservationReport report)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        writer.WriteStartObject();
        if (report.HasDrift is { } drift) writer.WriteBoolean("hasDrift", drift);
        writer.WriteBoolean("complete", report.Complete);
        WriteCoverage(writer, report.Coverage);
        writer.WriteStartArray("findings");
        foreach (var finding in report.Findings) WriteFinding(writer, finding);
        writer.WriteEndArray();
        writer.WriteStartArray("slices");
        foreach (var slice in report.Slices) WriteSlice(writer, slice);
        writer.WriteEndArray();
        writer.WriteStartArray("notEstablished");
        foreach (var statement in report.NotEstablished) writer.WriteStringValue(statement);
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Serializes the report as a JSON string.
    /// </summary>
    /// <param name="report">The comparison result.</param>
    /// <returns>The compact result JSON.</returns>
    public static string Write(ObservationReport report) => Encoding.UTF8.GetString(Serialize(report));

    static void WriteFinding(Utf8JsonWriter writer, ObservationFinding finding)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", finding.Kind.ToString());
        writer.WriteString("outcome", finding.Outcome.ToString());
        Text(writer, "reason", finding.Reason?.ToString());
        Text(writer, "construct", finding.Construct?.ToString());
        Address(writer, finding.Address);
        Text(writer, "slice", finding.Slice?.ToString());
        Text(writer, "eventTypeId", finding.EventTypeId);
        Number(writer, "generation", finding.Generation);
        if (finding.Producer is { } producer)
        {
            writer.WriteStartObject("producer");
            writer.WriteString("kind", producer.Kind.ToString());
            writer.WriteString("identifier", producer.Identifier);
            writer.WriteEndObject();
        }

        Text(writer, "consumer", finding.Consumer);
        Text(writer, "sequenceId", finding.SequenceId);
        Number(writer, "count", finding.Count);
        writer.WriteString("statement", finding.Statement);
        writer.WriteEndObject();
    }

    static void Address(Utf8JsonWriter writer, SemanticAddress? address)
    {
        if (address is null) return;
        writer.WriteStartObject("address");
        writer.WriteString("kind", address.Kind.ToString());
        writer.WriteStartArray("parts");
        foreach (var part in address.Parts)
        {
            writer.WriteStartObject();
            writer.WriteString("kind", part.Kind.ToString());
            writer.WriteString("key", part.Key);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    static void Text(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is not null) writer.WriteString(name, value);
    }

    static void Number(Utf8JsonWriter writer, string name, long? value)
    {
        if (value is { } number) writer.WriteNumber(name, number);
    }
}

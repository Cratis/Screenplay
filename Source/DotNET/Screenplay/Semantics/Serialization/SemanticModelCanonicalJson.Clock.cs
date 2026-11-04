// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Writes the application calendar and clock operands admitted in ESM v37 (decisions 0106, 0107 and 0132).
/// </summary>
/// <remarks>
/// Every v37 application states its effective zone, and <c>contextMembers</c> lists its declared members as <c>{ "name", "type": "timeZone" }</c> when it declares any: <c>timeZone</c> is <c>{ "id", "configurable" }</c> and defaults to a
/// non-configurable <c>"UTC"</c>. A command <c>calendar</c> carries exactly one of <c>timeZone</c> or <c>contextMember</c>.
/// A clock operand is <c>{ "kind": "today"|"now", "offset"?: { "amount", "unit": "day"|"hour"|"minute" }, "dateOf"?: true }</c>;
/// a zero offset in days and a false <c>dateOf</c> are omitted. Below v37 every clock member is refused, never dropped.
/// </remarks>
public static partial class SemanticModelCanonicalJson
{
    static readonly SemanticTimeZone _utc = new("UTC", false);

    static void RefuseUnpublishedClock(SemanticApplication application, SemanticVersion version)
    {
        if (!version.IsAtLeast(SemanticVersion.V37) && SemanticClockUsage.Uses(application))
        {
            throw new InvalidSemanticContract("The application calendar and clock operands require ESM v37.");
        }
    }

    static void WriteApplicationTimeZone(Utf8JsonWriter writer, SemanticApplication application, SemanticVersion version)
    {
        if (!version.IsAtLeast(SemanticVersion.V37)) return;
        var zone = application.TimeZone ?? _utc;
        writer.WriteStartObject("timeZone");
        CanonicalJson.WriteString(writer, "id", zone.Id);
        writer.WriteBoolean("configurable", zone.Configurable);
        writer.WriteEndObject();
        if (application.ContextMembers.IsDefaultOrEmpty) return;
        writer.WriteStartArray("contextMembers");
        foreach (var member in application.ContextMembers)
        {
            writer.WriteStartObject();
            CanonicalJson.WriteString(writer, "name", member);
            writer.WriteString("type", "timeZone");
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    static void WriteCommandCalendar(Utf8JsonWriter writer, SemanticCommandCalendar? calendar)
    {
        if (calendar is null) return;
        writer.WriteStartObject("calendar");
        if (calendar.TimeZone is { } zone) CanonicalJson.WriteString(writer, "timeZone", zone);
        else CanonicalJson.WriteString(writer, "contextMember", calendar.ContextMember!);
        writer.WriteEndObject();
    }

    static void WriteClockOperand(Utf8JsonWriter writer, SemanticClockOperand? clock)
    {
        if (clock is null) return;
        writer.WriteStartObject("clock");
        writer.WriteString("kind", clock.Kind == SemanticClockKind.Today ? "today" : "now");
        if (clock.Amount != 0 || clock.Unit != SemanticClockUnit.Day)
        {
            writer.WriteStartObject("offset");
            writer.WriteNumber("amount", clock.Amount);
            writer.WriteString("unit", clock.Unit switch
            {
                SemanticClockUnit.Day => "day",
                SemanticClockUnit.Hour => "hour",
                SemanticClockUnit.Minute => "minute",
                _ => throw new InvalidSemanticContract("Unknown clock unit.")
            });
            writer.WriteEndObject();
        }

        if (clock.DateOf) writer.WriteBoolean("dateOf", true);
        writer.WriteEndObject();
    }
}

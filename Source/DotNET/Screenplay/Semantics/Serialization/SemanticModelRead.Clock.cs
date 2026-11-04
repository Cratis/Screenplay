// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    internal static SemanticTimeZone ApplicationTimeZone(ref Utf8JsonReader reader)
    {
        Object(ref reader, "time zone");
        var seen = NewSeen();
        string? id = null;
        bool? configurable = null;
        while (NextProperty(ref reader, seen, "time zone") is { } property)
        {
            switch (property)
            {
                case "id": id = String(ref reader, property); break;
                case "configurable": configurable = Boolean(ref reader, property); break;
                default: throw Unknown(property, "time zone");
            }
        }

        Required(id is not null && configurable is not null, "time zone");
        return new(id!, configurable!.Value);
    }

    internal static string ContextMember(ref Utf8JsonReader reader)
    {
        Object(ref reader, "context member");
        var seen = NewSeen();
        string? name = null;
        string? type = null;
        while (NextProperty(ref reader, seen, "context member") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "type": type = String(ref reader, property); break;
                default: throw Unknown(property, "context member");
            }
        }

        Required(name is not null && type == "timeZone", "context member");
        return name!;
    }

    internal static SemanticCommandCalendar CommandCalendar(ref Utf8JsonReader reader)
    {
        Object(ref reader, "calendar");
        var seen = NewSeen();
        string? zone = null;
        string? member = null;
        while (NextProperty(ref reader, seen, "calendar") is { } property)
        {
            switch (property)
            {
                case "timeZone": zone = String(ref reader, property); break;
                case "contextMember": member = String(ref reader, property); break;
                default: throw Unknown(property, "calendar");
            }
        }

        Required((zone is null) != (member is null), "calendar");
        return new(zone, member);
    }

    internal static SemanticClockOperand ClockOperand(ref Utf8JsonReader reader)
    {
        Object(ref reader, "clock operand");
        var seen = NewSeen();
        SemanticClockKind? kind = null;
        (int Amount, SemanticClockUnit Unit) offset = (0, SemanticClockUnit.Day);
        var dateOf = false;
        while (NextProperty(ref reader, seen, "clock operand") is { } property)
        {
            switch (property)
            {
                case "kind": kind = ClockKind(String(ref reader, property)); break;
                case "offset": offset = Nested(ref reader, ClockOffset, property); break;
                case "dateOf": dateOf = OnlyTrue(ref reader, property, "clock operand"); break;
                default: throw Unknown(property, "clock operand");
            }
        }

        Required(kind is not null, "clock operand");
        return new(kind!.Value, offset.Amount, offset.Unit) { DateOf = dateOf };
    }

    static (int Amount, SemanticClockUnit Unit) ClockOffset(ref Utf8JsonReader reader)
    {
        Object(ref reader, "clock offset");
        var seen = NewSeen();
        int? amount = null;
        SemanticClockUnit? unit = null;
        while (NextProperty(ref reader, seen, "clock offset") is { } property)
        {
            switch (property)
            {
                case "amount": amount = Int32(ref reader, property); break;
                case "unit": unit = ClockUnit(String(ref reader, property)); break;
                default: throw Unknown(property, "clock offset");
            }
        }

        Required(IsWrittenOffset(amount, unit), "clock offset");
        return (amount!.Value, unit!.Value);
    }

    static SemanticClockKind ClockKind(string kind) => kind switch
    {
        "today" => SemanticClockKind.Today,
        "now" => SemanticClockKind.Now,
        _ => throw new InvalidSemanticContract("Unknown clock kind.")
    };

    static SemanticClockUnit ClockUnit(string unit) => unit switch
    {
        "day" => SemanticClockUnit.Day,
        "hour" => SemanticClockUnit.Hour,
        "minute" => SemanticClockUnit.Minute,
        _ => throw new InvalidSemanticContract("Unknown clock unit.")
    };

    // A zero offset in days is written as no offset, so it is never read back as one.
    static bool IsWrittenOffset(int? amount, SemanticClockUnit? unit) =>
        amount is not null && unit is not null && (amount != 0 || unit != SemanticClockUnit.Day);
}

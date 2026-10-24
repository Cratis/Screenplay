// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

public sealed partial class ScreenplayPrinter
{
    static void WriteTimeZone(ScreenplayWriter writer, TimeZoneSyntax? zone)
    {
        if (zone is null) return;
        writer.Line($"time zone {(zone.Configurable ? "configurable default " : string.Empty)}{StringLiteral.Quote(zone.Id)}");
        writer.Blank();
    }

    static void WriteCalendar(ScreenplayWriter writer, CommandCalendarSyntax? calendar)
    {
        if (calendar is null) return;
        writer.Line(calendar.Id is null ? $"calendar from $context.{calendar.ContextMember}" : $"calendar {StringLiteral.Quote(calendar.Id)}", calendar);
    }

    static void WriteGivenTimeZone(ScreenplayWriter writer, SpecificationTimeZoneSyntax? zone)
    {
        if (zone is null) return;
        writer.Line($"given time zone {StringLiteral.Quote(zone.Id)}", zone);
    }
}

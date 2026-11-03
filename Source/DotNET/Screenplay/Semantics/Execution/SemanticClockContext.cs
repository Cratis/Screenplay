// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution;

// One evaluation instant and the calendar zone resolved for it (decisions 0106 and 0107). Evaluating one instant per
// command keeps every clock operand of that command consistent.
internal sealed class SemanticClockContext(DateTimeOffset instant, TimeZoneInfo zone)
{
    internal DateTimeOffset Instant { get; } = instant;

    internal TimeZoneInfo Zone { get; } = zone;

    internal SemanticPrimitiveType Primitive(SemanticClockOperand operand) =>
        operand.Kind == SemanticClockKind.Today || operand.DateOf ? SemanticPrimitiveType.Date : SemanticPrimitiveType.DateTime;

    internal SemanticValue Value(SemanticClockOperand operand) => operand.Kind == SemanticClockKind.Today ? Today(operand) : Now(operand);

    // 'date of x': the local date of the instant x in the resolved zone.
    internal SemanticValue DateOf(SemanticValue instant) =>
        instant is SemanticTextValue text && SemanticDateComparisons.TryParseInstant(text.Value, out var moment)
            ? Date(LocalDate(moment))
            : throw new InvalidSemanticContract("'date of' requires a date-time with an explicit offset.");

    static SemanticValue Date(DateOnly date) => SemanticValue.Text(date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));

    SemanticValue Today(SemanticClockOperand operand)
    {
        if (operand.Unit != SemanticClockUnit.Day && operand.Amount != 0)
        {
            throw new InvalidSemanticContract("'today' is offset in whole days.");
        }

        return Date(LocalDate(Instant).AddDays(operand.Amount));
    }

    SemanticValue Now(SemanticClockOperand operand)
    {
        var moment = Offset(operand);
        return operand.DateOf ? Date(LocalDate(moment)) : SemanticValue.Text(moment.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
    }

    DateTimeOffset Offset(SemanticClockOperand operand) => operand.Unit switch
    {
        SemanticClockUnit.Hour => Instant.AddHours(operand.Amount),
        SemanticClockUnit.Minute => Instant.AddMinutes(operand.Amount),
        _ => Instant.AddHours(24d * operand.Amount)
    };

    DateOnly LocalDate(DateTimeOffset moment) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(moment, Zone).DateTime);
}

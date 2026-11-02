// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticClockContext;

public class when_evaluating_fixed_instants : Specification
{
    [Theory]
    [InlineData("2025-03-30T00:30:00Z", 1, SemanticClockUnit.Hour, "2025-03-30T01:30:00.0000000Z")]
    [InlineData("2025-10-26T00:30:00Z", 1, SemanticClockUnit.Hour, "2025-10-26T01:30:00.0000000Z")]
    [InlineData("2024-02-28T23:30:00Z", 1, SemanticClockUnit.Day, "2024-02-29T23:30:00.0000000Z")]
    [InlineData("2024-03-01T00:15:00+01:00", -30, SemanticClockUnit.Minute, "2024-02-29T22:45:00.0000000Z")]
    public void should_offset_now_by_elapsed_time(string instant, int amount, SemanticClockUnit unit, string expected)
    {
        var clock = Clock(instant, "Europe/Oslo");
        clock.Value(new(SemanticClockKind.Now, amount, unit)).ShouldEqual(SemanticValue.Text(expected));
    }

    // On the last Sundays of March and October, a local day and 24 elapsed hours are different.
    [Theory]
    [InlineData("2025-03-29T22:30:00Z", "2025-03-30", "2025-03-31")]
    [InlineData("2025-10-25T22:30:00Z", "2025-10-27", "2025-10-26")]
    public void should_distinguish_a_calendar_day_from_twenty_four_hours_across_dst(string instant, string tomorrow, string elapsedDate)
    {
        var clock = Clock(instant, "Europe/Oslo");
        clock.Value(new(SemanticClockKind.Today, 1, SemanticClockUnit.Day)).ShouldEqual(SemanticValue.Text(tomorrow));
        clock.Value(new(SemanticClockKind.Now, 1, SemanticClockUnit.Day) { DateOf = true }).ShouldEqual(SemanticValue.Text(elapsedDate));
    }

    [Theory]
    [InlineData("2024-02-28T23:30:00Z", "Europe/Oslo", "2024-02-29")]
    [InlineData("2025-03-01T18:45:00Z", "Asia/Kolkata", "2025-03-02")]
    [InlineData("2025-03-30T01:30:00Z", "Europe/Oslo", "2025-03-30")]
    [InlineData("2025-10-26T02:30:00+02:00", "Europe/Oslo", "2025-10-26")]
    [InlineData("2025-10-26T02:30:00+01:00", "Europe/Oslo", "2025-10-26")]
    public void should_take_the_local_date_of_an_explicit_instant(string instant, string zone, string expected)
    {
        var clock = Clock("2000-01-01T00:00:00Z", zone);
        clock.DateOf(SemanticValue.Text(instant)).ShouldEqual(SemanticValue.Text(expected));
    }

    static SemanticClockContext Clock(string instant, string zone) =>
        new(DateTimeOffset.Parse(instant, CultureInfo.InvariantCulture), TimeZoneInfo.FindSystemTimeZoneById(zone));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

/// <summary>
/// The clock triggers, which are what a reaction gains over a reactor: an automation whose input is the
/// passage of time rather than an event had no way to say so, so a canvas timer arrived with its schedule
/// dropped.
/// </summary>
public class when_compiling_a_reaction_driven_by_the_clock : given.a_compiler
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice Automation RunBilling
              reaction Billing
                every 15 minutes

            slice Automation Morning
              reaction MorningReport
                at 08:00

            slice Automation Weekly
              reaction WeeklyReport
                at 09:30 on Monday

            slice Automation Monthly
              reaction MonthlyClose
                at 00:00 on day 1

            slice Automation Daily
              reaction DailySweep
                every 1 day
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_read_the_interval_amount() => Interval("RunBilling").Amount.ShouldEqual(15);
    [Fact] void should_read_the_interval_unit() => Interval("RunBilling").Unit.ShouldEqual(IntervalUnit.Minutes);
    [Fact] void should_read_a_singular_interval() => Interval("Daily").Amount.ShouldEqual(1);
    [Fact] void should_read_a_singular_interval_unit() => Interval("Daily").Unit.ShouldEqual(IntervalUnit.Days);
    [Fact] void should_read_the_time_of_day() => Schedule("Morning").Time.ShouldEqual(new TimeOnly(8, 0));
    [Fact] void should_leave_a_daily_schedule_without_a_day() => (Schedule("Morning") is { DayOfWeek: null, DayOfMonth: null }).ShouldBeTrue();
    [Fact] void should_read_the_day_of_week() => Schedule("Weekly").DayOfWeek.ShouldEqual(DayOfWeek.Monday);
    [Fact] void should_read_the_time_alongside_the_day_of_week() => Schedule("Weekly").Time.ShouldEqual(new TimeOnly(9, 30));
    [Fact] void should_read_the_day_of_month() => Schedule("Monthly").DayOfMonth.ShouldEqual(1);

    IntervalTriggerSourceSyntax Interval(string slice) => (IntervalTriggerSourceSyntax)Source_(slice);

    ScheduleTriggerSourceSyntax Schedule(string slice) => (ScheduleTriggerSourceSyntax)Source_(slice);

    TriggerSourceSyntax Source_(string slice) =>
        _result.Value!.Modules.Single().Features.Single().Slices.Single(_ => _.Name == slice)
            .Reactions.Single().Triggers.Single().Source;
}

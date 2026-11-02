// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_clock_operands;

public class with_calendars_and_time_zones : given.a_clock_binder
{
    [Fact]
    void should_bind_the_application_time_zone_and_select_36()
    {
        var result = BindClock(Source(string.Empty, "time zone configurable default \"Europe/Oslo\"\n"));
        result.Success.ShouldBeTrue();
        result.Value!.Application.TimeZone.ShouldEqual(new SemanticTimeZone("Europe/Oslo", true));
        result.Value!.LanguageVersion.Major.ShouldEqual(36u);
    }

    [Fact]
    void should_bind_a_literal_command_calendar()
    {
        var result = BindClock(Source("        calendar \"Europe/Oslo\"\n"));
        Command(result.Value!).Calendar.ShouldEqual(new SemanticCommandCalendar("Europe/Oslo", null));
    }

    [Fact]
    void should_bind_a_calendar_from_the_trusted_context_member()
    {
        var result = BindClock(Source("        calendar from $context.timeZone\n"));
        Command(result.Value!).Calendar.ShouldEqual(new SemanticCommandCalendar(null, "timeZone"));
    }

    [Fact]
    void should_refuse_any_other_context_member()
    {
        var result = BindClock(Source("        calendar from $context.zone\n"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidCalendarContextMember);
    }

    [Theory]
    [InlineData("Mars/Olympus")]
    [InlineData("")]
    public void should_refuse_an_unknown_zone_without_falling_back_to_utc(string zone)
    {
        var result = BindClock(Source($"        calendar \"{zone}\"\n"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownTimeZone);
    }

    [Fact]
    void should_refuse_an_unknown_application_zone()
    {
        var result = BindClock(Source(string.Empty, "time zone \"Nowhere/Land\"\n"));
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownTimeZone);
    }

    [Fact]
    void should_bind_given_time_zone_on_a_specification()
    {
        const string spec = "      specification InOslo\n        given time zone \"Europe/Oslo\"\n        given clock \"2025-03-02T10:00:00Z\"\n        when clock \"2025-03-03T10:00:00Z\"\n        then Done\n";
        var result = BindClock(Source(string.Empty, extra: spec));
        result.Diagnostics.Select(diagnostic => diagnostic.Message).ShouldBeEmpty();
        result.Success.ShouldBeTrue();
        result.Value!.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().GivenTimeZone.ShouldEqual("Europe/Oslo");
    }
}

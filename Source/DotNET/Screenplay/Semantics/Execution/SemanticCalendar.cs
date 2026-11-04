// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

// Resolves the calendar zone a command evaluates its clock operands in (decision 0106). A zone that is required and
// missing, unknown or invalid is a failure with a message: it is never replaced by UTC or by the application zone.
internal static class SemanticCalendar
{
    internal static SemanticClockContext? Resolve(SemanticApplication application, SemanticCommand command, SemanticExecutionRequest request, out string? failure)
    {
        failure = null;
        if (request.Occurrence is null)
        {
            failure = "A command using a clock operand needs the instant of the command; the execution request supplies no occurrence.";
            return null;
        }

        var id = ZoneId(application, command, request, out failure);
        if (id is null) return null;
        if (!IanaTimeZones.TryFind(id, out var zone))
        {
            failure = $"The calendar time zone '{id}' is not a known IANA time zone.";
            return null;
        }

        return new SemanticClockContext(request.Occurrence.Occurred, zone);
    }

    // 'given time zone' supplies $context.timeZone and every declared member a command calendar reads (decision 0132 D2).
    internal static ImmutableDictionary<string, string> Given(IEnumerable<SemanticCommand> commands, string? zone)
    {
        var members = ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.Ordinal);
        if (zone is null) return members;
        foreach (var member in commands.Select(command => command.Calendar?.ContextMember).OfType<string>())
        {
            if (member != SemanticTypedContextCatalog.CalendarContextMember) members = members.SetItem(member, zone);
        }

        return members;
    }

    static string? ZoneId(SemanticApplication application, SemanticCommand command, SemanticExecutionRequest request, out string? failure)
    {
        failure = null;
        if (command.Calendar is { } calendar) return CommandZoneId(command, calendar, request, out failure);

        // The specification pins the zone at the application tier, above deployment configuration; a command calendar (above) still wins.
        return request.ScenarioTimeZone ?? ApplicationZoneId(application, request, out failure);
    }

    static string? CommandZoneId(SemanticCommand command, SemanticCommandCalendar calendar, SemanticExecutionRequest request, out string? failure)
    {
        failure = null;
        if (calendar.TimeZone is { } literal) return literal;
        var member = calendar.ContextMember ?? SemanticTypedContextCatalog.CalendarContextMember;
        var supplied = member == SemanticTypedContextCatalog.CalendarContextMember ? request.ContextTimeZone : request.ContextTimeZones.GetValueOrDefault(member);
        if (!string.IsNullOrWhiteSpace(supplied)) return supplied;
        failure = $"Command '{command.Name}' reads its calendar from $context.{member}, and the execution request supplies none.";
        return null;
    }

    // A configured zone is supplied (even when empty) or not (null); a supplied one is never replaced by the declared zone.
    static string? ApplicationZoneId(SemanticApplication application, SemanticExecutionRequest request, out string? failure)
    {
        failure = null;
        if (application.TimeZone is not { Configurable: true } || request.ConfiguredTimeZone is not { } configured) return application.TimeZone?.Id ?? IanaTimeZones.Utc;
        if (!string.IsNullOrWhiteSpace(configured)) return configured;
        failure = "The configured application time zone is empty; supply an IANA time zone id or none.";
        return null;
    }
}

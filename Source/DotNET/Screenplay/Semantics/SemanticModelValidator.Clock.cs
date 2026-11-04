// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static void ValidateClockVersion(SemanticApplication application, SemanticVersion version)
    {
        ValidateFeatureVersion(
            SemanticClockUsage.Uses(application),
            version,
            SemanticVersion.V37,
            "An ESM v37 model must read the application calendar or a clock.",
            "The application calendar and clock operands require ESM v37.");
        if (!version.IsAtLeast(SemanticVersion.V37)) return;
        ValidateClockGraph(application);
    }

    // The writer and the reader accept any string; the graph rules of decisions 0106, 0107 and 0132 are checked here once.
    static void ValidateClockGraph(SemanticApplication application)
    {
        if (application.TimeZone is { } zone && !IanaTimeZones.IsKnown(zone.Id)) throw new InvalidSemanticContract("The application time zone must be an IANA time zone id.");
        var members = application.ContextMembers.IsDefault ? [] : application.ContextMembers;
        ValidateContextMembers(members);
        foreach (var calendar in SlicesOf(application).SelectMany(slice => slice.Commands).Select(command => command.Calendar).OfType<SemanticCommandCalendar>())
        {
            ValidateCommandCalendar(calendar, members);
        }
    }

    static void ValidateContextMembers(ImmutableArray<string> members)
    {
        if (members.Any(member => member.Length == 0 || member == SemanticTypedContextCatalog.CalendarContextMember)) throw new InvalidSemanticContract("A declared context member must be named and must not reuse the fixed timeZone member.");
        if (members.Distinct(StringComparer.Ordinal).Count() != members.Length) throw new InvalidSemanticContract("Declared context members must be unique.");
    }

    static void ValidateCommandCalendar(SemanticCommandCalendar calendar, ImmutableArray<string> members)
    {
        if ((calendar.TimeZone is null) == (calendar.ContextMember is null)) throw new InvalidSemanticContract("A command calendar names exactly one time zone or context member.");
        if (calendar.TimeZone is { } literal && !IanaTimeZones.IsKnown(literal)) throw new InvalidSemanticContract("A command calendar time zone must be an IANA time zone id.");
        if (calendar.ContextMember is { } member && member != SemanticTypedContextCatalog.CalendarContextMember && !members.Contains(member, StringComparer.Ordinal))
        {
            throw new InvalidSemanticContract("A command calendar reads an undeclared context member.");
        }
    }
}

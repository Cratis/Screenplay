// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

internal static class PublicEventInvariants
{
    internal static string? Error(SyntaxNode node) => node switch
    {
        EventSyntax @event => EventError(@event.Visibility, @event.Origin),
        ImportSyntax import => EventError(import.Visibility, import.Origin) ??
            (import.Visibility == EventVisibility.Public && import.Origin is null ? "A public contract import requires an origin." : null),
        SliceSyntax { Direction: not null } slice when slice.Type != SliceType.Translate => "Only a Translate slice may declare a direction.",
        SliceSyntax { Direction: { } direction } when !Enum.IsDefined(direction) => "A translation direction must be Inbound or Outbound.",
        _ => null
    };

    static string? EventError(EventVisibility visibility, string? origin)
    {
        if (!Enum.IsDefined(visibility)) return "Event visibility must be Private or Public.";
        if (origin is not null && string.IsNullOrWhiteSpace(origin)) return "An event or import origin must be a nonblank quoted string.";
        if (origin is not null && visibility != EventVisibility.Public) return "An event or import with an origin is a public contract.";

        return null;
    }
}

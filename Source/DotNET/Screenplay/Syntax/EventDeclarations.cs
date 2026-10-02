// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Enumerates slice-owned event declarations without changing their authoring structure.
/// </summary>
public static class EventDeclarations
{
    /// <summary>
    /// Gets standalone and command-inline events, including every declared generation.
    /// </summary>
    /// <param name="slice">The owning slice.</param>
    /// <returns>The event declarations owned by the slice.</returns>
    public static IEnumerable<EventSyntax> In(SliceSyntax slice) =>
        slice.Events.Concat(slice.Commands.SelectMany(command => command.Produces).Select(production => production.InlineEvent).OfType<EventSyntax>());
}

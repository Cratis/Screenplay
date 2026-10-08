// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

static class ScreenContainers
{
    internal static IEnumerable<IEnumerable<ScreenDirectiveSyntax>> Children(ScreenDirectiveSyntax directive) => directive switch
    {
        ScreenTemplateReferenceSyntax template => template.Slots.Select(slot => slot.Directives),
        ScreenSlotSyntax slot => [slot.Directives],
        ScreenSectionSyntax section => [section.Directives],
        _ => []
    };

    internal static IEnumerable<ScreenDirectiveSyntax> All(IEnumerable<ScreenDirectiveSyntax> directives) =>
        directives.SelectMany(directive => new[] { directive }.Concat(Children(directive).SelectMany(All)));
}

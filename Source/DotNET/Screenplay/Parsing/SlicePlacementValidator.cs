// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates that every <c>shows</c> placement of a slice refers to one declaration elsewhere in the application.
/// </summary>
internal static class SlicePlacementValidator
{
    /// <summary>
    /// Validates the placements of every slice.
    /// </summary>
    /// <param name="slices">Every slice of the application.</param>
    /// <param name="names">The <see cref="DocumentNames"/> declared by the application.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    public static void Validate(IReadOnlyList<SliceSyntax> slices, DocumentNames names, ParserContext context)
    {
        foreach (var slice in slices)
        {
            var seen = new HashSet<(SlicePlacementKind, string)>();
            foreach (var placement in slice.Placements)
            {
                Validate(slice, placement, names, seen, context);
            }
        }
    }

    static void Validate(SliceSyntax slice, SlicePlacementSyntax placement, DocumentNames names, HashSet<(SlicePlacementKind, string)> seen, ParserContext context)
    {
        var keyword = SlicePlacementParser.Keyword(placement.Kind);
        var declared = placement.Kind == SlicePlacementKind.Event ? names.Events : names.ReadModels;
        if (!declared.Contains(placement.Name))
        {
            context.Warning(DiagnosticCodes.UnknownPlacedElement, $"Slice '{slice.Name}' shows {keyword} '{placement.Name}', which the application does not declare", placement.Location);
            return;
        }

        if (DeclaresItself(slice, placement))
        {
            context.Warning(DiagnosticCodes.RedundantSlicePlacement, $"Slice '{slice.Name}' declares {keyword} '{placement.Name}' itself, so it is already shown there", placement.Location);
            return;
        }

        if (!seen.Add((placement.Kind, placement.Name)))
        {
            context.Warning(DiagnosticCodes.RedundantSlicePlacement, $"Slice '{slice.Name}' shows {keyword} '{placement.Name}' more than once", placement.Location);
        }
    }

    static bool DeclaresItself(SliceSyntax slice, SlicePlacementSyntax placement) => placement.Kind == SlicePlacementKind.Event
        ? EventDeclarations.In(slice).Any(@event => @event.Name == placement.Name)
        : slice.Projections.SelectMany(ScreenplayValidator.ProjectionBuiltReadModelNames)
            .Concat((slice.Reducers ?? []).Select(reducer => reducer.ReadModel))
            .Concat((slice.ReadModels ?? []).Select(readModel => readModel.Name))
            .Contains(placement.Name, StringComparer.Ordinal);
}

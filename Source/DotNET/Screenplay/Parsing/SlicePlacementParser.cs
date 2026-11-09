// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses a slice's <c>shows</c> line - a linked copy of an event or read model declared elsewhere.
/// </summary>
internal static partial class SlicePlacementParser
{
    /// <summary>
    /// Gets the keyword a <see cref="SlicePlacementKind"/> is written with after <c>shows</c>.
    /// </summary>
    /// <param name="kind">The <see cref="SlicePlacementKind"/>.</param>
    /// <returns>The keyword.</returns>
    public static string Keyword(SlicePlacementKind kind) => kind == SlicePlacementKind.ReadModel ? "readmodel" : "event";

    /// <summary>
    /// Parses a <c>shows</c> line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/>.</param>
    /// <returns>The <see cref="SlicePlacementSyntax"/>, or null when the line is malformed.</returns>
    public static SlicePlacementSyntax? Parse(ParserContext context, SourceLine line)
    {
        var match = PlacementRegex().Match(line.Content);
        if (!match.Success)
        {
            context.SkipBlock(line.Indent);
            context.Error(DiagnosticCodes.InvalidSlicePlacement, $"Invalid placement '{line.Content}' - expected 'shows event <Name>' or 'shows readmodel <Name>'", line.Location);
            return null;
        }

        var kind = match.Groups[1].Value == "readmodel" ? SlicePlacementKind.ReadModel : SlicePlacementKind.Event;
        return new(kind, match.Groups[2].Value, line.Location);
    }

    [GeneratedRegex(@"^shows\s+(event|readmodel)\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex PlacementRegex();
}

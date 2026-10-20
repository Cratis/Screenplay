// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses top level <c>seed</c> blocks - events to seed into the event store per event source id.
/// </summary>
internal static partial class SeedParser
{
    /// <summary>
    /// Parses a seed block from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>seed</c> header.</param>
    /// <returns>The parsed <see cref="SeedSyntax"/>.</returns>
    public static SeedSyntax Parse(ParserContext context, SourceLine header)
    {
        var headerMatch = HeaderRegex().Match(header.Content);
        if (!headerMatch.Success)
        {
            context.Error(DiagnosticCodes.InvalidSeedDeclaration, $"Invalid seed declaration '{header.Content}' - expected 'seed' or 'seed \"<name>\"'", header.Location);
            context.SkipBlock(header.Indent);
            return new([], header.Location);
        }

        var groups = new List<SeedGroupSyntax>();
        (int X, int Y)? position = null;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "position")
            {
                position = ScreenplayParser.ParseCanvasPair(context, line, "position", "x", "y", position, allowNegative: true) ?? position;
                continue;
            }

            var match = ForRegex().Match(line.Content);
            if (!match.Success)
            {
                context.Error(DiagnosticCodes.InvalidSeedGroup, $"Invalid seed group '{line.Content}' - expected 'for \"<event source id>\"'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            groups.Add(new(StringLiteral.Unescape(match.Groups[1].Value), ParseEvents(context, line), line.Location));
        }

        return new(groups, header.Location) { Name = headerMatch.Groups[1].Success ? StringLiteral.Unescape(headerMatch.Groups[1].Value) : null, X = position?.X, Y = position?.Y };
    }

    static List<SeedEventSyntax> ParseEvents(ParserContext context, SourceLine group)
    {
        var events = new List<SeedEventSyntax>();
        while (context.TryPeekChild(group.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (!EventNameRegex().IsMatch(line.Content))
            {
                context.Error(DiagnosticCodes.InvalidSeedEvent, $"Invalid seed event '{line.Content}' - expected an event type name", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            events.Add(new(line.Content, ParseProperties(context, line), line.Location));
        }

        return events;
    }

    static List<PropertyMappingSyntax> ParseProperties(ParserContext context, SourceLine parent)
    {
        var properties = new List<PropertyMappingSyntax>();
        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var match = MappingRegex().Match(child.Content);
            if (!match.Success)
            {
                context.Error(DiagnosticCodes.InvalidSeedPropertyAssignment, $"Invalid property assignment '{child.Content}' - expected '<property> = <value>'", child.Location);
                continue;
            }

            properties.Add(ExpressionParser.ParseMapping(context, match.Groups[1].Value, match.Groups[2], child));
        }

        return properties;
    }

    [GeneratedRegex("^seed(?:\\s+\"(" + StringLiteral.BodyPattern + ")\")?$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex("^for\\s+\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.NonBacktracking)]
    private static partial Regex ForRegex();

    [GeneratedRegex(@"^[A-Z]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex EventNameRegex();

    [GeneratedRegex(@"^([\w.]+)\s*=(?:\s+(?<2>.+)|(?<2>[^=>\n].*))$", RegexOptions.NonBacktracking)]
    private static partial Regex MappingRegex();
}

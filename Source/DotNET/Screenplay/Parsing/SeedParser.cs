// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

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
        if (header.Content != "seed")
        {
            context.Error($"Invalid seed declaration '{header.Content}' - expected 'seed'", header.Location);
            context.SkipBlock(header.Indent);
            return new([], header.Location);
        }

        var groups = new List<SeedGroupSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var match = ForRegex().Match(line.Content);
            if (!match.Success)
            {
                context.Error($"Invalid seed group '{line.Content}' - expected 'for \"<event source id>\"'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            groups.Add(new(match.Groups[1].Value, ParseEvents(context, line), line.Location));
        }

        return new(groups, header.Location);
    }

    static List<SeedEventSyntax> ParseEvents(ParserContext context, SourceLine group)
    {
        var events = new List<SeedEventSyntax>();
        while (context.TryPeekChild(group.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (!EventNameRegex().IsMatch(line.Content))
            {
                context.Error($"Invalid seed event '{line.Content}' - expected an event type name", line.Location);
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
                context.Error($"Invalid property assignment '{child.Content}' - expected '<property> = <value>'", child.Location);
                continue;
            }

            properties.Add(new(match.Groups[1].Value, ExpressionParser.ParseMappingSource(match.Groups[2].Value, child.Location), child.Location));
        }

        return properties;
    }

    [GeneratedRegex("^for\\s+\"([^\"]*)\"$", RegexOptions.None, 1000)]
    private static partial Regex ForRegex();

    [GeneratedRegex(@"^[A-Z]\w*$", RegexOptions.None, 1000)]
    private static partial Regex EventNameRegex();

    [GeneratedRegex(@"^([\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();
}

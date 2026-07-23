// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>event</c> declarations with their properties.
/// </summary>
internal static partial class EventParser
{
    /// <summary>
    /// Parses an event from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>event</c> header.</param>
    /// <returns>The parsed <see cref="EventSyntax"/>.</returns>
    public static EventSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error($"Invalid event declaration '{header.Content}' - expected 'event <Name>'", header.Location);
        }

        var properties = new List<PropertySyntax>();
        var tags = new List<TagSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "tag")
            {
                if (TagParser.Parse(context, line) is { } tag)
                {
                    tags.Add(tag);
                }
            }
            else if (PropertyLineParser.TryParse(line) is { } property)
            {
                properties.Add(property);
            }
            else
            {
                context.Error($"Invalid property '{line.Content}' - expected '<name> <Type>'", line.Location);
            }
        }

        return new(name.Groups[1].Value, properties, header.Location, tags);
    }

    [GeneratedRegex(@"^event\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();
}

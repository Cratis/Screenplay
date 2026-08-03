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
                WarnOnAmbiguousTag(context, line);
                if (TagParser.Parse(context, line) is { } tag)
                {
                    tags.Add(tag);
                }
            }
            else if (PropertyLineParser.TryParse(line) is { } property)
            {
                if (property.IsIdentifier)
                {
                    context.Error($"Property '{property.Name}' of event '{name.Groups[1].Value}' cannot be marked identifier - an event never carries its event source id", line.Location);
                    property = property with { IsIdentifier = false };
                }

                properties.Add(property);
            }
            else
            {
                context.Error($"Invalid property '{line.Content}' - expected '<name> <Type>'", line.Location);
            }
        }

        return new(name.Groups[1].Value, properties, header.Location, tags);
    }

    /// <summary>
    /// Warns when a <c>tag</c> line reads as a property declaration - <c>tag TagType</c> is a static tag with
    /// the value <c>TagType</c>, but it has the exact shape of a property named <c>tag</c>.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report the diagnostic to.</param>
    /// <param name="line">The <see cref="SourceLine"/> holding the <c>tag</c> line.</param>
    /// <remarks>
    /// The tag wins, because that is what the line has always meant. A lowercase value such as
    /// <c>tag audit</c> does not read as a type reference and is left alone.
    /// </remarks>
    static void WarnOnAmbiguousTag(ParserContext context, SourceLine line)
    {
        var value = line.Content["tag".Length..].Trim();
        if (!TypeShapedRegex().IsMatch(value))
        {
            return;
        }

        context.Warning(
            $"'{line.Content}' declares a static tag with the value '{value}', not a property named 'tag' - write 'tag \"{value}\"' for the tag, or '@{line.Content}' for the property",
            line.Location);
    }

    [GeneratedRegex(@"^event\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^[A-Z]\w*(?:\[\])?\??$", RegexOptions.None, 1000)]
    private static partial Regex TypeShapedRegex();
}

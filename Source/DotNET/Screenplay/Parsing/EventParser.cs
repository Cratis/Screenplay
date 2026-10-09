// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

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
            context.Error(DiagnosticCodes.InvalidEventDeclaration, $"Invalid event declaration '{header.Content}' - expected '[public] event <Name> [generation <N>] [from \"<origin>\"]'",  header.Location);
        }

        var origin = name.Groups["origin"].Success ? StringLiteral.Unescape(name.Groups["origin"].Value) : null;
        var visibility = name.Groups["public"].Success || origin is not null ? EventVisibility.Public : EventVisibility.Private;
        if (origin is not null && string.IsNullOrWhiteSpace(origin))
        {
            context.Error(DiagnosticCodes.InvalidEventDeclaration, "An event origin must be a nonblank quoted string", header.Location);
        }

        var hasGenerationMarker = name.Groups[2].Success;
        var generation = 1u;
        if (hasGenerationMarker && (!uint.TryParse(name.Groups[2].Value, out generation) || generation == 0 || generation == uint.MaxValue))
        {
            context.Error(DiagnosticCodes.InvalidEventGeneration, $"Event '{name.Groups[1].Value}' must declare a generation between 1 and {uint.MaxValue - 1}", header.Location);
            generation = 1;
        }

        var metadata = new EventMetadataParser(name.Groups[1].Value);
        var properties = new List<PropertySyntax>();
        var tags = new List<TagSyntax>();
        var directiveLocations = new Dictionary<string, SourceLocation>();
        FileReferenceSyntax? file = null;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (FileReferenceParser.IsDirectiveAmongProperties(line))
            {
                file = FileReferenceParser.ParseReplacing(context, line, file, directiveLocations);
            }
            else if (LineText.FirstWord(line.Content) == "tag")
            {
                WarnOnAmbiguousTag(context, line);
                if (TagParser.Parse(context, line) is { } tag)
                {
                    tags.Add(tag);
                }
            }
            else if (PropertyLineParser.Parse(context, line) is { } property)
            {
                if (property.IsIdentifier)
                {
                    context.Error(DiagnosticCodes.IdentifierOnEventProperty, $"Property '{property.Name}' of event '{name.Groups[1].Value}' cannot be marked identifier - an event never carries its event source id", line.Location);
                    property = property with { IsIdentifier = false };
                }

                properties.Add(property);
            }
            else if (!metadata.TryParse(context, line))
            {
                context.Error(DiagnosticCodes.InvalidPropertyDeclaration, $"Invalid property '{line.Content}' - expected '<name> <Type>'", line.Location);
            }
        }

        return metadata.Apply(new(name.Groups[1].Value, properties, header.Location, tags) { File = file, Visibility = visibility, Origin = origin, Generation = generation, HasGenerationMarker = hasGenerationMarker, DirectiveLocations = directiveLocations });
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
            DiagnosticCodes.TagPropertyReadAsTag,
            $"'{line.Content}' declares a static tag with the value '{value}', not a property named 'tag' - write 'tag \"{value}\"' for the tag, or '@{line.Content}' for the property",
            line.Location);
    }

    [GeneratedRegex(@"^(?:(?<public>public)\s+)?event\s+([A-Za-z_]\w*)(?:\s+generation\s+([0-9]+))?(?:\s+from\s+""(?<origin>" + StringLiteral.BodyPattern + @")"")?$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^[A-Z]\w*(?:\[\])?(?:\?|\s+optional)?$", RegexOptions.None, 1000)]
    private static partial Regex TypeShapedRegex();
}

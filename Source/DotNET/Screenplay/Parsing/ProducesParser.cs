// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>produces</c> declarations - the events a command or a reaction appends, where they land, and
/// how their properties are filled.
/// </summary>
/// <remarks>
/// Shared rather than written twice: a reaction produces events the same way a command does, so the two say
/// the same thing with the same words and a reader learns the construct once.
/// </remarks>
internal static partial class ProducesParser
{
    /// <summary>
    /// Parses a produces declaration from its already consumed line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>produces</c> keyword.</param>
    /// <param name="inCommand">Whether the production belongs to a command.</param>
    /// <param name="propertyNameComparer">The inline property-name comparer, defaulting to ordinal comparison.</param>
    /// <returns>The parsed <see cref="ProducesSyntax"/>, or <c>null</c> when the declaration is malformed.</returns>
    public static ProducesSyntax? Parse(ParserContext context, SourceLine line, bool inCommand = false, IEqualityComparer<string>? propertyNameComparer = null)
    {
        if (InlineHeaderRegex().Match(line.Content) is { Success: true } inline)
        {
            if (!inCommand)
            {
                context.Error(DiagnosticCodes.InlineEventOutsideCommand, "Inline events can only be declared inside commands", line.Location);
                context.SkipBlock(line.Indent);
                return null;
            }

            if (inline.Groups[2].Success)
            {
                context.Error(DiagnosticCodes.InlineEventGeneration, "Inline events are generation 1 - extract the event before declaring generations", line.Location);
            }

            return ParseInline(context, line, inline.Groups[1].Value, propertyNameComparer);
        }

        var conditional = ProducesWhenRegex().Match(line.Content);
        if (conditional.Success)
        {
            var condition = ConditionParser.Parse(context, conditional.Groups[1].Value, line.Location);
            if (!context.TryPeekChild(line.Indent, out var eventLine) || !EventNameRegex().IsMatch(eventLine.Content))
            {
                context.Error(DiagnosticCodes.ProducesWhenWithoutEvent, "Expected an event name on the line after 'produces when'", line.Location);
                context.SkipBlock(line.Indent);
                return null;
            }

            context.Reader.TakeSignificant();
            var body = ParseBody(context, eventLine);
            context.SkipBlock(line.Indent);
            return new ProducesSyntax(eventLine.Content, condition, body.Mappings, line.Location, body.Tags, body.For)
            {
                DirectiveLocations = new Dictionary<string, SourceLocation> { ["event"] = eventLine.Location }
            };
        }

        var unconditional = ProducesRegex().Match(line.Content);
        if (!unconditional.Success)
        {
            context.Error(DiagnosticCodes.InvalidProducesDeclaration, $"Invalid produces declaration '{line.Content}' - expected 'produces <EventType>' or 'produces when <condition>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var unconditionalBody = ParseBody(context, line);
        return new(unconditional.Groups[1].Value, null, unconditionalBody.Mappings, line.Location, unconditionalBody.Tags, unconditionalBody.For);
    }

    /// <summary>
    /// Parses the body of a produces declaration - where the event lands, its tags and its mappings.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="parent">The <see cref="SourceLine"/> the body belongs to.</param>
    /// <returns>What the body declared.</returns>
    /// <remarks>
    /// <c>for</c> is an indented line rather than an argument on the header. That is the ruling on
    /// <see href="https://github.com/Cratis/Screenplay/issues/33">#33</see> - indentation over a
    /// parenthesised call - and it puts the target beside the mappings that fill the event rather than
    /// out past the end of the line.
    /// </remarks>
    static (List<PropertyMappingSyntax> Mappings, List<TagSyntax> Tags, ExpressionSyntax? For) ParseBody(ParserContext context, SourceLine parent)
    {
        var mappings = new List<PropertyMappingSyntax>();
        var tags = new List<TagSyntax>();
        ExpressionSyntax? target = null;

        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (RejectReserved(context, child))
            {
                continue;
            }

            if (LineText.FirstWord(child.Content) == "tag")
            {
                if (TagParser.Parse(context, child) is { } tag)
                {
                    tags.Add(tag);
                }

                continue;
            }

            if (ForRegex().Match(child.Content) is { Success: true } forMatch)
            {
                if (target is not null)
                {
                    context.Error(
                        DiagnosticCodes.DuplicateProducesTarget,
                        $"'{parent.Content}' already declares where it lands - an event is appended to one event source",
                        child.Location);
                    continue;
                }

                target = ExpressionParser.ParseMappingSource(context, forMatch.Groups[1].Value, child.Location);
                continue;
            }

            var match = MappingRegex().Match(child.Content);
            if (!match.Success)
            {
                context.Error(DiagnosticCodes.InvalidPropertyMapping, $"Invalid property mapping '{child.Content}' - expected '<property> = <source>'", child.Location);
                continue;
            }

            mappings.Add(ExpressionParser.ParseMapping(context, LineText.Unescape(match.Groups[1].Value), match.Groups[2], child));
        }

        return (mappings, tags, target);
    }

    static ProducesSyntax ParseInline(ParserContext context, SourceLine header, string name, IEqualityComparer<string>? propertyNameComparer)
    {
        var metadata = new EventMetadataParser(name);
        var properties = new List<PropertySyntax>();
        var propertyNames = new HashSet<string>(propertyNameComparer ?? StringComparer.Ordinal);
        var mappings = new List<PropertyMappingSyntax>();
        var tags = new List<TagSyntax>();
        ExpressionSyntax? target = null;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var keyword = LineText.FirstWord(line.Content);
            if (RejectReserved(context, line))
            {
                continue;
            }

            if (keyword == "generation" || keyword == "origin")
            {
                context.Error(
                    keyword == "generation" ? DiagnosticCodes.InlineEventGeneration : DiagnosticCodes.ReservedProductionMetadata,
                    keyword == "generation" ? "Inline events are generation 1 - extract the event before declaring generations" : "An inline event is local to its command and cannot declare origin",
                    line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            if (keyword == "tag")
            {
                if (TagParser.Parse(context, line) is { } tag) tags.Add(tag);
                continue;
            }

            if (ForRegex().Match(line.Content) is { Success: true } forMatch)
            {
                if (target is not null)
                {
                    context.Error(DiagnosticCodes.DuplicateProducesTarget, $"'{header.Content}' already declares where it lands - an event is appended to one event source", line.Location);
                }
                else
                {
                    target = ExpressionParser.ParseMappingSource(context, forMatch.Groups[1].Value, line.Location);
                }

                continue;
            }

            var match = TypedMappingRegex().Match(line.Content);
            if (match.Success && PropertyLineParser.Parse(context, line with { Content = match.Groups[1].Value.TrimEnd() }) is { } property)
            {
                if (property.IsIdentifier)
                {
                    context.Error(DiagnosticCodes.IdentifierOnEventProperty, $"Property '{property.Name}' of event '{name}' cannot be marked identifier - an event never carries its event source id", line.Location);
                    property = property with { IsIdentifier = false };
                }

                if (!propertyNames.Add(property.Name))
                {
                    context.Error(DiagnosticCodes.DuplicateDeclaration, $"Event '{name}' already declares property '{property.Name}'", line.Location);
                }

                properties.Add(property);
                mappings.Add(ExpressionParser.ParseMapping(context, property.Name, match.Groups[2], line));
            }
            else if (!metadata.TryParse(context, line))
            {
                context.Error(DiagnosticCodes.InvalidPropertyMapping, $"Invalid inline property mapping '{line.Content}' - expected '<property> <Type> = <source>'", line.Location);
            }
        }

        return new(name, null, mappings, header.Location, [], target)
        {
            InlineEvent = metadata.Apply(new(name, properties, header.Location, tags))
        };
    }

    static bool RejectReserved(ParserContext context, SourceLine line)
    {
        var keyword = LineText.FirstWord(line.Content);
        if (keyword is not ("namespace" or "sequence" or "correlation" or "causation" or "causedBy" or "occurred"))
        {
            return false;
        }

        context.Error(DiagnosticCodes.ReservedProductionMetadata, $"'{keyword}' is system-assigned production metadata and cannot be supplied here", line.Location);
        context.SkipBlock(line.Indent);
        return true;
    }

    [GeneratedRegex(@"^produces\s+event\s+([A-Za-z_]\w*)(?:\s+(generation)(?:\s+.*)?)?$", RegexOptions.None, 1000)]
    private static partial Regex InlineHeaderRegex();

    [GeneratedRegex(@"^(.+?)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex TypedMappingRegex();

    [GeneratedRegex(@"^produces\s+when\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex ProducesWhenRegex();

    [GeneratedRegex(@"^produces\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ProducesRegex();

    [GeneratedRegex(@"^([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex EventNameRegex();

    [GeneratedRegex(@"^for\s+(\S.*)$", RegexOptions.None, 1000)]
    private static partial Regex ForRegex();

    [GeneratedRegex(@"^(@?[\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();
}

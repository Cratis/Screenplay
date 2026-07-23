// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>command</c> declarations with their properties, authorization, validation and event production.
/// </summary>
internal static partial class CommandParser
{
    /// <summary>
    /// Parses a command from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>command</c> header.</param>
    /// <returns>The parsed <see cref="CommandSyntax"/>.</returns>
    public static CommandSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error($"Invalid command declaration '{header.Content}' - expected 'command <Name>'", header.Location);
        }

        var properties = new List<PropertySyntax>();
        AuthorizeSyntax? authorize = null;
        var validations = new List<ValidateSyntax>();
        var produces = new List<ProducesSyntax>();
        HandlerSyntax? handler = null;
        ConcurrencySyntax? concurrency = null;
        string? description = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, line, description, $"Command '{name.Groups[1].Value}'");
                    break;
                case "authorize":
                    authorize = AuthorizeParser.Parse(context, line);
                    break;
                case "concurrency":
                    concurrency = ParseConcurrency(context, line, concurrency, name.Groups[1].Value);
                    break;
                case "validate":
                    if (ValidateParser.Parse(context, line) is { } validate)
                    {
                        validations.Add(validate);
                    }

                    break;
                case "produces":
                    if (ParseProduces(context, line) is { } production)
                    {
                        produces.Add(production);
                    }

                    break;
                case "handler":
                    handler = ParseHandler(context, line);
                    break;
                default:
                    if (PropertyLineParser.TryParse(line) is { } property)
                    {
                        properties.Add(property);
                    }
                    else
                    {
                        context.Error($"Unexpected '{line.Content}' in command body", line.Location);
                        context.SkipBlock(line.Indent);
                    }

                    break;
            }
        }

        if (handler is not null && produces.Count > 0)
        {
            context.Error($"Command '{name.Groups[1].Value}' cannot declare both 'produces' and 'handler'", header.Location);
        }

        return new(name.Groups[1].Value, properties, authorize, validations, produces, handler, header.Location, concurrency, description);
    }

    static ConcurrencySyntax? ParseConcurrency(ParserContext context, SourceLine line, ConcurrencySyntax? existing, string commandName)
    {
        if (line.Content != "concurrency")
        {
            context.Error($"Invalid concurrency declaration '{line.Content}' - expected 'concurrency'", line.Location);
            context.SkipBlock(line.Indent);
            return existing;
        }

        if (existing is not null)
        {
            context.Error($"Command '{commandName}' already declares a concurrency block - a command can have at most one", line.Location);
            context.SkipBlock(line.Indent);
            return existing;
        }

        var eventSource = false;
        string? eventSourceType = null;
        string? eventStreamType = null;
        string? eventStreamId = null;
        List<string>? eventTypes = null;

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "eventSource":
                    eventSource = ParseEventSourceDimension(context, child, eventSource);
                    break;
                case "sourceType":
                    eventSourceType = ParseNamedDimension(context, child, "sourceType", eventSourceType);
                    break;
                case "streamType":
                    eventStreamType = ParseNamedDimension(context, child, "streamType", eventStreamType);
                    break;
                case "streamId":
                    eventStreamId = ParseNamedDimension(context, child, "streamId", eventStreamId);
                    break;
                case "events":
                    eventTypes = ParseEventsDimension(context, child, eventTypes);
                    break;
                default:
                    context.Error($"Unexpected '{child.Content}' in concurrency block - expected eventSource, sourceType, streamType, streamId or events", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(eventSource, eventSourceType, eventStreamType, eventStreamId, eventTypes ?? [], line.Location);
    }

    static bool ParseEventSourceDimension(ParserContext context, SourceLine line, bool existing)
    {
        if (line.Content != "eventSource")
        {
            context.Error($"Invalid eventSource dimension '{line.Content}' - expected 'eventSource'", line.Location);
            return existing;
        }

        if (existing)
        {
            context.Error("Duplicate 'eventSource' in concurrency block - each dimension can appear at most once", line.Location);
        }

        return true;
    }

    static string? ParseNamedDimension(ParserContext context, SourceLine line, string dimension, string? existing)
    {
        var match = ConcurrencyDimensionRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid {dimension} dimension '{line.Content}' - expected '{dimension} <Name>'", line.Location);
            return existing;
        }

        if (existing is not null)
        {
            context.Error($"Duplicate '{dimension}' in concurrency block - each dimension can appear at most once", line.Location);
            return existing;
        }

        return match.Groups[2].Value;
    }

    static List<string>? ParseEventsDimension(ParserContext context, SourceLine line, List<string>? existing)
    {
        if (existing is not null)
        {
            context.Error("Duplicate 'events' in concurrency block - each dimension can appear at most once", line.Location);
            return existing;
        }

        var names = line.Content["events".Length..].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0 || Array.Exists(names, name => !EventNameRegex().IsMatch(name)))
        {
            context.Error($"Invalid events dimension '{line.Content}' - expected 'events <EventType>[, <EventType>]*'", line.Location);
            return existing;
        }

        return [.. names];
    }

    static ProducesSyntax? ParseProduces(ParserContext context, SourceLine line)
    {
        var conditional = ProducesWhenRegex().Match(line.Content);
        if (conditional.Success)
        {
            var condition = ConditionParser.Parse(context, conditional.Groups[1].Value, line.Location);
            if (!context.TryPeekChild(line.Indent, out var eventLine) || !EventNameRegex().IsMatch(eventLine.Content))
            {
                context.Error("Expected an event name on the line after 'produces when'", line.Location);
                context.SkipBlock(line.Indent);
                return null;
            }

            context.Reader.TakeSignificant();
            var (mappings, tags) = ParseMappingsAndTags(context, eventLine);
            context.SkipBlock(line.Indent);
            return new(eventLine.Content, condition, mappings, line.Location, tags);
        }

        var unconditional = ProducesRegex().Match(line.Content);
        if (!unconditional.Success)
        {
            context.Error($"Invalid produces declaration '{line.Content}' - expected 'produces <EventType>' or 'produces when <condition>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var (unconditionalMappings, unconditionalTags) = ParseMappingsAndTags(context, line);
        return new(unconditional.Groups[1].Value, null, unconditionalMappings, line.Location, unconditionalTags);
    }

    static (List<PropertyMappingSyntax> Mappings, List<TagSyntax> Tags) ParseMappingsAndTags(ParserContext context, SourceLine parent)
    {
        var mappings = new List<PropertyMappingSyntax>();
        var tags = new List<TagSyntax>();
        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "tag")
            {
                if (TagParser.Parse(context, child) is { } tag)
                {
                    tags.Add(tag);
                }

                continue;
            }

            var match = MappingRegex().Match(child.Content);
            if (!match.Success)
            {
                context.Error($"Invalid property mapping '{child.Content}' - expected '<property> = <source>'", child.Location);
                continue;
            }

            mappings.Add(new(match.Groups[1].Value, ExpressionParser.ParseMappingSource(match.Groups[2].Value, child.Location), child.Location));
        }

        return (mappings, tags);
    }

    static HandlerSyntax? ParseHandler(ParserContext context, SourceLine line)
    {
        if (!context.TryPeekChild(line.Indent, out var body))
        {
            context.Error("Expected a 'file' directive or an inline code block in the handler", line.Location);
            return null;
        }

        context.Reader.TakeSignificant();
        if (LineText.FirstWord(body.Content) == "file")
        {
            return new(new(body.Content["file".Length..].Trim(), body.Location), null, line.Location);
        }

        if (CodeBlockParser.Languages.Contains(body.Content))
        {
            var code = CodeBlockParser.Parse(context, body.Content, body);
            return code is null ? null : new HandlerSyntax(null, code, line.Location);
        }

        context.Error($"Unexpected '{body.Content}' in handler - expected 'file <path>' or an inline code block", body.Location);
        context.SkipBlock(line.Indent);
        return null;
    }

    [GeneratedRegex(@"^command\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^produces\s+when\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex ProducesWhenRegex();

    [GeneratedRegex(@"^produces\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ProducesRegex();

    [GeneratedRegex(@"^([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex EventNameRegex();

    [GeneratedRegex(@"^(sourceType|streamType|streamId)\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ConcurrencyDimensionRegex();

    [GeneratedRegex(@"^([\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
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
            context.Error(DiagnosticCodes.InvalidCommandDeclaration, $"Invalid command declaration '{header.Content}' - expected 'command <Name>'", header.Location);
        }

        var properties = new List<PropertySyntax>();
        AuthorizeSyntax? authorize = null;
        var validations = new List<ValidateSyntax>();
        var produces = new List<ProducesSyntax>();
        var reads = new List<ReadsSyntax>();
        HandlerSyntax? handler = null;
        ConcurrencySyntax? concurrency = null;
        string? description = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                // The bare directives below cannot take a type reference, so a line that has property shape
                // is a property no matter which keyword it starts with - 'description String' declares a
                // property called description. Only the directives that do take an identifier operand
                // ('authorize', 'produces') stay ambiguous, and those use the '@' escape.
                case "description" or "handler" or "concurrency" when PropertyLineParser.TryParse(line) is { } named:
                    AddProperty(context, properties, named, name.Groups[1].Value);
                    break;
                case "validate" when line.Content != "validate csharp" && PropertyLineParser.TryParse(line) is { } validated:
                    AddProperty(context, properties, validated, name.Groups[1].Value);
                    break;
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
                case "reads":
                    if (ReadsParser.Parse(context, line) is { } read)
                    {
                        AddReads(context, reads, read, name.Groups[1].Value);
                    }

                    break;
                case "handler":
                    handler = ParseHandler(context, line);
                    break;
                default:
                    if (PropertyLineParser.TryParse(line) is { } property)
                    {
                        AddProperty(context, properties, property, name.Groups[1].Value);
                    }
                    else
                    {
                        context.Error(DiagnosticCodes.UnknownCommandDirective, $"Unexpected '{line.Content}' in command body", line.Location);
                        context.SkipBlock(line.Indent);
                    }

                    break;
            }
        }

        if (handler is not null && produces.Count > 0)
        {
            context.Error(DiagnosticCodes.CommandWithProducesAndHandler, $"Command '{name.Groups[1].Value}' cannot declare both 'produces' and 'handler'", header.Location);
        }

        return new(name.Groups[1].Value, properties, authorize, validations, produces, handler, header.Location, concurrency, description, reads);
    }

    /// <summary>
    /// Adds a read model to the command, keeping at most one declaration per read model.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="reads">The read models declared so far.</param>
    /// <param name="read">The <see cref="ReadsSyntax"/> to add.</param>
    /// <param name="commandName">The name of the command, used in diagnostics.</param>
    /// <remarks>
    /// A read model names what it holds, so reading it twice says nothing the first declaration did not - and
    /// two declarations disagreeing about the key would leave a consumer no way to choose. The first wins.
    /// </remarks>
    static void AddReads(ParserContext context, List<ReadsSyntax> reads, ReadsSyntax read, string commandName)
    {
        if (reads.Exists(existing => existing.ReadModel == read.ReadModel))
        {
            context.Error(
                DiagnosticCodes.DuplicateReads,
                $"Command '{commandName}' already reads '{read.ReadModel}'",
                read.Location);
            return;
        }

        reads.Add(read);
    }

    /// <summary>
    /// Adds a property to the command, keeping at most one of them marked as the identifier.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="properties">The properties parsed so far.</param>
    /// <param name="property">The <see cref="PropertySyntax"/> to add.</param>
    /// <param name="commandName">The name of the command, used in diagnostics.</param>
    /// <remarks>
    /// The identifier is what a runtime resolves the event source id from, so a second one would leave it
    /// with no way to choose. The first declaration wins and the rest are reported.
    /// </remarks>
    static void AddProperty(ParserContext context, List<PropertySyntax> properties, PropertySyntax property, string commandName)
    {
        if (property.IsIdentifier && properties.Find(existing => existing.IsIdentifier) is { } identifier)
        {
            context.Error(DiagnosticCodes.DuplicateCommandIdentifier, $"Command '{commandName}' already marks '{identifier.Name}' as identifier - only one property can be the identifier", property.Location);
            property = property with { IsIdentifier = false };
        }

        properties.Add(property);
    }

    static ConcurrencySyntax? ParseConcurrency(ParserContext context, SourceLine line, ConcurrencySyntax? existing, string commandName)
    {
        if (line.Content != "concurrency")
        {
            context.Error(DiagnosticCodes.InvalidConcurrencyDeclaration, $"Invalid concurrency declaration '{line.Content}' - expected 'concurrency'", line.Location);
            context.SkipBlock(line.Indent);
            return existing;
        }

        if (existing is not null)
        {
            context.Error(DiagnosticCodes.DuplicateConcurrencyBlock, $"Command '{commandName}' already declares a concurrency block - a command can have at most one", line.Location);
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
                    context.Error(DiagnosticCodes.UnknownConcurrencyDimension, $"Unexpected '{child.Content}' in concurrency block - expected eventSource, sourceType, streamType, streamId or events", child.Location);
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
            context.Error(DiagnosticCodes.InvalidConcurrencyDimension, $"Invalid eventSource dimension '{line.Content}' - expected 'eventSource'", line.Location);
            return existing;
        }

        if (existing)
        {
            context.Error(DiagnosticCodes.DuplicateConcurrencyDimension, "Duplicate 'eventSource' in concurrency block - each dimension can appear at most once", line.Location);
        }

        return true;
    }

    static string? ParseNamedDimension(ParserContext context, SourceLine line, string dimension, string? existing)
    {
        var match = ConcurrencyDimensionRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidConcurrencyDimension, $"Invalid {dimension} dimension '{line.Content}' - expected '{dimension} <Name>'", line.Location);
            return existing;
        }

        if (existing is not null)
        {
            context.Error(DiagnosticCodes.DuplicateConcurrencyDimension, $"Duplicate '{dimension}' in concurrency block - each dimension can appear at most once", line.Location);
            return existing;
        }

        return match.Groups[2].Value;
    }

    static List<string>? ParseEventsDimension(ParserContext context, SourceLine line, List<string>? existing)
    {
        if (existing is not null)
        {
            context.Error(DiagnosticCodes.DuplicateConcurrencyDimension, "Duplicate 'events' in concurrency block - each dimension can appear at most once", line.Location);
            return existing;
        }

        var names = line.Content["events".Length..].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0 || Array.Exists(names, name => !EventNameRegex().IsMatch(name)))
        {
            context.Error(DiagnosticCodes.InvalidConcurrencyDimension, $"Invalid events dimension '{line.Content}' - expected 'events <EventType>[, <EventType>]*'", line.Location);
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
                context.Error(DiagnosticCodes.ProducesWhenWithoutEvent, "Expected an event name on the line after 'produces when'", line.Location);
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
            context.Error(DiagnosticCodes.InvalidProducesDeclaration, $"Invalid produces declaration '{line.Content}' - expected 'produces <EventType>' or 'produces when <condition>'", line.Location);
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
                context.Error(DiagnosticCodes.InvalidPropertyMapping, $"Invalid property mapping '{child.Content}' - expected '<property> = <source>'", child.Location);
                continue;
            }

            mappings.Add(new(LineText.Unescape(match.Groups[1].Value), ExpressionParser.ParseMappingSource(context, match.Groups[2].Value, child.Location), child.Location));
        }

        return (mappings, tags);
    }

    static HandlerSyntax? ParseHandler(ParserContext context, SourceLine line)
    {
        if (!context.TryPeekChild(line.Indent, out var body))
        {
            context.Error(DiagnosticCodes.HandlerWithoutImplementation, "Expected a 'file' directive or an inline code block in the handler", line.Location);
            return null;
        }

        context.Reader.TakeSignificant();
        if (LineText.FirstWord(body.Content) == "file")
        {
            return new(new(body.Content["file".Length..].Trim(), body.Location), null, line.Location);
        }

        if (context.Languages.InlineLanguages.Contains(body.Content))
        {
            var code = CodeBlockParser.Parse(context, body.Content, body);
            return code is null ? null : new HandlerSyntax(null, code, line.Location);
        }

        context.Error(DiagnosticCodes.UnknownHandlerDirective, $"Unexpected '{body.Content}' in handler - expected 'file <path>' or an inline code block", body.Location);
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

    [GeneratedRegex(@"^(@?[\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();
}

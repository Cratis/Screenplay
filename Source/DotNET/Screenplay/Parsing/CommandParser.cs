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

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "authorize":
                    authorize = AuthorizeParser.Parse(context, line);
                    break;
                case "validate":
                    if (ParseValidate(context, line) is { } validate)
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

        return new(name.Groups[1].Value, properties, authorize, validations, produces, handler, header.Location);
    }

    static ValidateSyntax? ParseValidate(ParserContext context, SourceLine line)
    {
        if (line.Content == "validate")
        {
            var rules = new List<ValidationRuleSyntax>();
            while (context.TryPeekChild(line.Indent, out var child))
            {
                context.Reader.TakeSignificant();
                if (ValidationRuleParser.Parse(context, child) is { } rule)
                {
                    rules.Add(rule);
                }
            }

            return new DeclarativeValidateSyntax(rules, line.Location);
        }

        if (line.Content == "validate csharp")
        {
            var code = CodeBlockParser.Parse(context, "csharp", line);
            return code is null ? null : new CodeValidateSyntax(code, line.Location);
        }

        context.Error($"Invalid validate declaration '{line.Content}' - expected 'validate' or 'validate csharp'", line.Location);
        context.SkipBlock(line.Indent);
        return null;
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
            var mappings = ParseMappings(context, eventLine);
            context.SkipBlock(line.Indent);
            return new(eventLine.Content, condition, mappings, line.Location);
        }

        var unconditional = ProducesRegex().Match(line.Content);
        if (!unconditional.Success)
        {
            context.Error($"Invalid produces declaration '{line.Content}' - expected 'produces <EventType>' or 'produces when <condition>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        return new(unconditional.Groups[1].Value, null, ParseMappings(context, line), line.Location);
    }

    static List<PropertyMappingSyntax> ParseMappings(ParserContext context, SourceLine parent)
    {
        var mappings = new List<PropertyMappingSyntax>();
        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var match = MappingRegex().Match(child.Content);
            if (!match.Success)
            {
                context.Error($"Invalid property mapping '{child.Content}' - expected '<property> = <source>'", child.Location);
                continue;
            }

            mappings.Add(new(match.Groups[1].Value, ExpressionParser.ParseMappingSource(match.Groups[2].Value, child.Location), child.Location));
        }

        return mappings;
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

    [GeneratedRegex(@"^([\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();
}

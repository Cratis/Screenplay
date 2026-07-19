// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>specification</c> declarations - the Given/When/Then test scenario sub-language.
/// </summary>
internal static partial class SpecificationParser
{
    /// <summary>
    /// Parses the specifications of a standalone specification document.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <returns>The parsed <see cref="SpecificationSyntax">specifications</see>.</returns>
    public static IReadOnlyList<SpecificationSyntax> ParseDocument(ParserContext context)
    {
        var specifications = new List<SpecificationSyntax>();
        while (context.Reader.PeekSignificant() is { } line)
        {
            if (line.Content.StartsWith("specification", StringComparison.Ordinal))
            {
                context.Reader.TakeSignificant();
                specifications.Add(Parse(context, line));
            }
            else
            {
                context.Error($"Expected 'specification', got '{LineText.FirstWord(line.Content)}'", line.Location);
                context.Reader.TakeSignificant();
                context.SkipBlock(line.Indent);
            }
        }

        if (specifications.Count == 0 && context.Diagnostics.Count == 0)
        {
            context.Error("Document must contain at least one specification", SourceLocation.Start);
        }

        return specifications;
    }

    /// <summary>
    /// Parses a specification from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>specification</c> header.</param>
    /// <returns>The parsed <see cref="SpecificationSyntax"/>.</returns>
    public static SpecificationSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        var name = string.Empty;

        if (!match.Success)
        {
            context.Error($"Invalid specification declaration '{header.Content}' - expected 'specification <Name>'", header.Location);
        }
        else
        {
            name = match.Groups[1].Value;
        }

        var given = new List<SpecificationEventSyntax>();
        SpecificationCommandSyntax? when = null;
        var thenEvents = new List<SpecificationEventSyntax>();
        var thenErrors = new List<SpecificationErrorSyntax>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "given":
                    if (ParseEventReference(context, line, GivenRegex(), "given") is { } givenEvent)
                    {
                        given.Add(givenEvent);
                    }

                    break;
                case "when":
                    if (when is not null)
                    {
                        context.Error($"Specification '{name}' already declares a 'when' - a specification can have at most one", line.Location);
                        context.SkipBlock(line.Indent);
                        break;
                    }

                    when = ParseWhen(context, line);
                    break;
                case "then":
                    ParseThen(context, line, thenEvents, thenErrors);
                    break;
                default:
                    context.Error($"Unexpected '{LineText.FirstWord(line.Content)}' in specification body", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(name, given, when, thenEvents, thenErrors, header.Location);
    }

    static SpecificationCommandSyntax? ParseWhen(ParserContext context, SourceLine line)
    {
        var match = WhenRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid 'when' declaration '{line.Content}' - expected 'when <CommandType>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        return new(match.Groups[1].Value, ParseValues(context, line), line.Location);
    }

    static void ParseThen(ParserContext context, SourceLine line, List<SpecificationEventSyntax> thenEvents, List<SpecificationErrorSyntax> thenErrors)
    {
        var errorMatch = ThenErrorRegex().Match(line.Content);
        if (errorMatch.Success)
        {
            thenErrors.Add(new(errorMatch.Groups[1].Value, line.Location));
            return;
        }

        if (ParseEventReference(context, line, ThenEventRegex(), "then") is { } thenEvent)
        {
            thenEvents.Add(thenEvent);
        }
    }

    static SpecificationEventSyntax? ParseEventReference(ParserContext context, SourceLine line, Regex regex, string keyword)
    {
        var match = regex.Match(line.Content);
        if (!match.Success)
        {
            context.Error($"Invalid '{keyword}' declaration '{line.Content}' - expected '{keyword} <EventType>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        return new(match.Groups[1].Value, ParseValues(context, line), line.Location);
    }

    static List<PropertyMappingSyntax> ParseValues(ParserContext context, SourceLine parent)
    {
        var values = new List<PropertyMappingSyntax>();
        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var match = MappingRegex().Match(child.Content);
            if (!match.Success)
            {
                context.Error($"Invalid property mapping '{child.Content}' - expected '<property> = <value>'", child.Location);
                continue;
            }

            values.Add(new(match.Groups[1].Value, ExpressionParser.ParseMappingSource(match.Groups[2].Value, child.Location), child.Location));
        }

        return values;
    }

    [GeneratedRegex(@"^specification\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^given\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex GivenRegex();

    [GeneratedRegex(@"^when\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex WhenRegex();

    [GeneratedRegex(@"^then\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ThenEventRegex();

    [GeneratedRegex("^then\\s+error\\s+\"([^\"]*)\"$", RegexOptions.None, 1000)]
    private static partial Regex ThenErrorRegex();

    [GeneratedRegex(@"^([\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();
}

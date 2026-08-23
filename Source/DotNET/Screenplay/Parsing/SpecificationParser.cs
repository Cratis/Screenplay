// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Text;

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
                context.Error(DiagnosticCodes.ExpectedSpecification, $"Expected 'specification', got '{LineText.FirstWord(line.Content)}'", line.Location);
                context.Reader.TakeSignificant();
                context.SkipBlock(line.Indent);
            }
        }

        if (specifications.Count == 0 && context.Diagnostics.Count == 0)
        {
            context.Error(DiagnosticCodes.SpecificationDocumentWithoutSpecification, "Document must contain at least one specification", SourceLocation.Start);
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
            context.Error(DiagnosticCodes.InvalidSpecificationDeclaration, $"Invalid specification declaration '{header.Content}' - expected 'specification <Name>'", header.Location);
        }
        else
        {
            name = match.Groups[1].Value;
        }

        var given = new List<SpecificationEventSyntax>();
        var givenReadModels = new List<SpecificationReadModelSyntax>();
        SpecificationCommandSyntax? when = null;
        var thenEvents = new List<SpecificationEventSyntax>();
        var thenReadModels = new List<SpecificationReadModelSyntax>();
        var thenQueries = new List<SpecificationQuerySyntax>();
        var thenErrors = new List<SpecificationErrorSyntax>();
        FileReferenceSyntax? file = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (FileReferenceParser.IsDirective(line))
            {
                file = FileReferenceParser.Parse(context, line);
                continue;
            }

            switch (LineText.FirstWord(line.Content))
            {
                case "given":
                    if (ReadModelPrefixRegex().IsMatch(line.Content))
                    {
                        if (ParseReadModel(context, line, GivenReadModelRegex(), "given") is { } givenReadModel)
                        {
                            givenReadModels.Add(givenReadModel);
                        }
                    }
                    else if (ParseEventReference(context, line, GivenRegex(), "given") is { } givenEvent)
                    {
                        given.Add(givenEvent);
                    }

                    break;
                case "when":
                    if (when is not null)
                    {
                        context.Error(DiagnosticCodes.DuplicateSpecificationWhen, $"Specification '{name}' already declares a 'when' - a specification can have at most one", line.Location);
                        context.SkipBlock(line.Indent);
                        break;
                    }

                    when = ParseWhen(context, line);
                    break;
                case "then":
                    ParseThen(context, line, thenEvents, thenReadModels, thenQueries, thenErrors);
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownSpecificationDirective, $"Unexpected '{LineText.FirstWord(line.Content)}' in specification body", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(name, given, when, thenEvents, thenErrors, header.Location, givenReadModels, thenReadModels)
        {
            File = file,
            ThenQueries = thenQueries
        };
    }

    static SpecificationCommandSyntax? ParseWhen(ParserContext context, SourceLine line)
    {
        var match = WhenRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationWhen, $"Invalid 'when' declaration '{line.Content}' - expected 'when <CommandType>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        return new(match.Groups[1].Value, ParseValues(context, line), line.Location);
    }

    static void ParseThen(
        ParserContext context,
        SourceLine line,
        List<SpecificationEventSyntax> thenEvents,
        List<SpecificationReadModelSyntax> thenReadModels,
        List<SpecificationQuerySyntax> thenQueries,
        List<SpecificationErrorSyntax> thenErrors)
    {
        // A bare 'then error' states the operation is rejected without naming a reason - the reason a
        // recovered specification usually carries in its name rather than in an assertion.
        if (line.Content == "then error")
        {
            thenErrors.Add(new(null, line.Location));
            return;
        }

        var errorMatch = ThenErrorRegex().Match(line.Content);
        if (errorMatch.Success)
        {
            thenErrors.Add(new(StringLiteral.Unescape(errorMatch.Groups[1].Value), line.Location));
            return;
        }

        if (LineText.FirstWord(line.Content["then".Length..].Trim()) == "error")
        {
            context.Error(DiagnosticCodes.InvalidThenError, $"Invalid 'then error' declaration '{line.Content}' - expected 'then error' or 'then error \"<reason>\"'", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        if (ThenQueryPrefixRegex().IsMatch(line.Content))
        {
            if (ParseQuery(context, line) is { } query)
            {
                thenQueries.Add(query);
            }

            return;
        }

        if (ReadModelPrefixRegex().IsMatch(line.Content))
        {
            if (ParseReadModel(context, line, ThenReadModelRegex(), "then") is { } thenReadModel)
            {
                thenReadModels.Add(thenReadModel);
            }

            return;
        }

        if (ParseEventReference(context, line, ThenEventRegex(), "then") is { } thenEvent)
        {
            thenEvents.Add(thenEvent);
        }
    }

    static SpecificationQuerySyntax? ParseQuery(ParserContext context, SourceLine line)
    {
        var match = ThenQueryRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationQuery, $"Invalid 'then query' declaration '{line.Content}' - expected 'then query <Query>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var arguments = new List<PropertyMappingSyntax>();
        var results = new List<SpecificationQueryResultSyntax>();
        var hasArguments = false;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (child.Content)
            {
                case "arguments":
                    if (hasArguments)
                    {
                        context.Error(DiagnosticCodes.DuplicateSpecificationQueryArguments, $"Query assertion '{match.Groups[1].Value}' already declares arguments", child.Location);
                        context.SkipBlock(child.Indent);
                        break;
                    }

                    hasArguments = true;
                    arguments.AddRange(ParseValues(context, child));
                    break;
                case "result":
                    results.Add(new(ParseValues(context, child), child.Location));
                    break;
                default:
                    context.Error(
                        DiagnosticCodes.UnknownSpecificationQueryDirective,
                        $"Unexpected '{LineText.FirstWord(child.Content)}' in 'then query' body - expected arguments or result",
                        child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(match.Groups[1].Value, arguments, results, line.Location);
    }

    static SpecificationReadModelSyntax? ParseReadModel(ParserContext context, SourceLine line, Regex regex, string keyword)
    {
        var match = regex.Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidReadModelStep, $"Invalid '{keyword} readmodel' declaration '{line.Content}' - expected '{keyword} readmodel <ReadModelType>'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        return new(match.Groups[1].Value, ParseValues(context, line), line.Location);
    }

    static SpecificationEventSyntax? ParseEventReference(ParserContext context, SourceLine line, Regex regex, string keyword)
    {
        var match = regex.Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidEventStep, $"Invalid '{keyword}' declaration '{line.Content}' - expected '{keyword} <EventType>'", line.Location);
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
                context.Error(DiagnosticCodes.InvalidSpecificationValue, $"Invalid property mapping '{child.Content}' - expected '<property> = <value>'", child.Location);
                continue;
            }

            values.Add(new(match.Groups[1].Value, ExpressionParser.ParseMappingSource(context, match.Groups[2].Value, child.Location), child.Location));
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

    [GeneratedRegex(@"^then\s+query\b", RegexOptions.None, 1000)]
    private static partial Regex ThenQueryPrefixRegex();

    [GeneratedRegex(@"^then\s+query\s+([A-Z]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex ThenQueryRegex();

    [GeneratedRegex(@"^(?:given|then)\s+readmodel\b", RegexOptions.None, 1000)]
    private static partial Regex ReadModelPrefixRegex();

    [GeneratedRegex(@"^given\s+readmodel\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex GivenReadModelRegex();

    [GeneratedRegex(@"^then\s+readmodel\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ThenReadModelRegex();

    [GeneratedRegex("^then\\s+error\\s+\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.None, 1000)]
    private static partial Regex ThenErrorRegex();

    [GeneratedRegex(@"^([\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();
}

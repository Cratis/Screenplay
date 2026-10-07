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
        var examples = new List<SpecificationExampleSyntax>();
        while (context.Reader.PeekSignificant() is { } line)
        {
            if (line.Content.StartsWith("specification", StringComparison.Ordinal))
            {
                context.Reader.TakeSignificant();
                specifications.Add(Parse(context, line));
            }
            else if (LineText.FirstWord(line.Content) == "example")
            {
                context.Reader.TakeSignificant();
                examples.Add(ParseExample(context, line));
            }
            else
            {
                context.Error(DiagnosticCodes.ExpectedSpecification, $"Expected 'specification', got '{LineText.FirstWord(line.Content)}'", line.Location);
                context.Reader.TakeSignificant();
                SkipBody(context, line.Indent);
            }
        }

        if (specifications.Count == 0 && context.Diagnostics.Count == 0)
        {
            context.Error(DiagnosticCodes.SpecificationDocumentWithoutSpecification, "Document must contain at least one specification", SourceLocation.Start);
        }

        return [.. specifications.Select(specification => specification with { Examples = examples })];
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
        var givenOperationFailures = new List<SpecificationOperationFailureSyntax>();
        var thenOperations = new List<SpecificationOperationSyntax>();
        var thenCompensated = new List<SpecificationCompensatedSyntax>();
        var givenReadModels = new List<SpecificationReadModelSyntax>();
        SpecificationCommandSyntax? when = null;
        SpecificationEventSyntax? whenAppended = null;
        SpecificationRedeliverySyntax? whenRedelivered = null;
        var whenDeclared = false;
        var eventsInAnyOrder = false;
        var thenNoEvents = false;
        SourceLocation? eventsInAnyOrderLocation = null;
        var thenEvents = new List<SpecificationEventSyntax>();
        var thenReadModels = new List<SpecificationReadModelSyntax>();
        var thenAbsentReadModels = new List<SpecificationAbsentReadModelSyntax>();
        var thenQueries = new List<SpecificationQuerySyntax>();
        var thenErrors = new List<SpecificationErrorSyntax>();
        SpecificationCallerSyntax? caller = null;
        SpecificationDeniedSyntax? denied = null;
        SpecificationReturnSyntax? thenReturns = null;
        FileReferenceSyntax? file = null;
        var directiveLocations = new Dictionary<string, SourceLocation>();
        SpecificationClockSyntax? givenClock = null;
        var givenCaptures = new List<SpecificationCaptureSyntax>();
        SpecificationClockSyntax? whenClock = null;
        SpecificationTriggerSyntax? whenTrigger = null;
        SpecificationCaptureSyntax? whenCapture = null;
        SpecificationWhenQuerySyntax? whenQuery = null;
        var thenResults = new List<SpecificationQueryResultSyntax>();
        SpecificationNoResultSyntax? thenNoResult = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (FileReferenceParser.IsDirective(line))
            {
                file = FileReferenceParser.ParseReplacing(context, line, file, directiveLocations);
                continue;
            }

            if (TryParseOperationStep(context, line, givenOperationFailures, thenOperations, thenCompensated)) continue;

            // Absence assertions admit any whitespace after 'then'; every other directive keeps its space-separated first word.
            switch (ThenNoPrefixRegex().IsMatch(line.Content) ? "then" : LineText.FirstWord(line.Content))
            {
                case "given":
                    if (KeywordRegex("given", "clock").IsMatch(line.Content))
                    {
                        if (givenClock is not null)
                        {
                            context.Error(DiagnosticCodes.InvalidSpecificationClock, "A specification states its clock at most once.", line.Location);
                            SkipBody(context, line.Indent);
                        }
                        else
                        {
                            givenClock = ParseClock(context, line, "given");
                        }
                    }
                    else if (KeywordRegex("given", "capture").IsMatch(line.Content))
                    {
                        if (ParseCapture(context, line, "given") is { } capture)
                        {
                            givenCaptures.Add(capture);
                        }
                    }
                    else if (line.Content.StartsWith("given caller", StringComparison.Ordinal))
                    {
                        if (caller is not null)
                        {
                            context.Error(DiagnosticCodes.DuplicateSpecificationCallerOrDenied, "A specification has at most one 'given caller' block.", line.Location);
                            SkipBody(context, line.Indent);
                        }
                        else
                        {
                            caller = ParseCaller(context, line);
                        }
                    }
                    else if (ReadModelPrefixRegex().IsMatch(line.Content))
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
                    if (whenDeclared)
                    {
                        context.Error(
                            whenAppended is not null || line.Content.StartsWith("when append", StringComparison.Ordinal)
                                ? DiagnosticCodes.ConflictingSpecificationActions : DiagnosticCodes.DuplicateSpecificationWhen,
                            $"Specification '{name}' already declares a 'when' - a specification can have at most one",
                            line.Location);
                        SkipBody(context, line.Indent);
                        break;
                    }

                    whenDeclared = true;
                    if (line.Content.StartsWith("when append", StringComparison.Ordinal))
                    {
                        whenAppended = ParseEventReference(context, line, WhenAppendRegex(), "when append");
                    }
                    else if (WhenRedeliveredPrefixRegex().IsMatch(line.Content))
                    {
                        whenRedelivered = ParseRedelivery(context, line);
                    }
                    else if (KeywordRegex("when", "clock").IsMatch(line.Content))
                    {
                        whenClock = ParseClock(context, line, "when");
                    }
                    else if (KeywordRegex("when", "trigger").IsMatch(line.Content))
                    {
                        whenTrigger = ParseTrigger(context, line);
                    }
                    else if (KeywordRegex("when", "capture").IsMatch(line.Content))
                    {
                        whenCapture = ParseCapture(context, line, "when");
                    }
                    else if (KeywordRegex("when", "query").IsMatch(line.Content))
                    {
                        whenQuery = ParseWhenQuery(context, line);
                    }
                    else
                    {
                        when = ParseWhen(context, line);
                    }
                    break;
                case "then":
                    if (ThenNoEventsPrefixRegex().IsMatch(line.Content))
                    {
                        if (line.Content != "then no events" || thenNoEvents)
                        {
                            context.Error(DiagnosticCodes.InvalidNoEventsExpectation, "Expected one 'then no events' directive.", line.Location);
                        }
                        else
                        {
                            thenNoEvents = true;
                            directiveLocations["then no events"] = line.Location;
                        }

                        if (context.TryPeekChild(line.Indent, out var child))
                        {
                            context.Error(DiagnosticCodes.InvalidNoEventsExpectation, "'then no events' cannot have child mappings.", child.Location);
                        }

                        SkipBody(context, line.Indent);
                    }
                    else if (ThenReturnsPrefixRegex().IsMatch(line.Content))
                    {
                        var expectation = ParseReturn(context, line);
                        if (thenReturns is not null)
                        {
                            context.Error(DiagnosticCodes.InvalidReturnExpectation, "A specification declares at most one return expectation.", line.Location);
                        }
                        else
                        {
                            thenReturns = expectation;
                        }
                    }
                    else if (line.Content.StartsWith("then events", StringComparison.Ordinal))
                    {
                        if (line.Content != "then events in any order" || eventsInAnyOrder)
                        {
                            context.Error(DiagnosticCodes.InvalidSpecificationEventOrder, "Expected one 'then events in any order' directive.", line.Location);
                        }
                        else
                        {
                            eventsInAnyOrder = true;
                            eventsInAnyOrderLocation = line.Location;
                        }
                        SkipBody(context, line.Indent);
                    }
                    else if (KeywordRegex("then", "result").IsMatch(line.Content))
                    {
                        if (ParseResult(context, line) is { } result)
                        {
                            thenResults.Add(result);
                        }
                    }
                    else if (NoResultRegex().IsMatch(line.Content))
                    {
                        if (line.Content != "then no result" || thenNoResult is not null)
                        {
                            context.Error(DiagnosticCodes.InvalidSpecificationQueryAction, "Expected one 'then no result' directive.", line.Location);
                        }
                        else
                        {
                            thenNoResult = new(line.Location);
                        }

                        SkipBody(context, line.Indent);
                    }
                    else if (line.Content.StartsWith("then denied", StringComparison.Ordinal))
                    {
                        if (line.Content != "then denied")
                        {
                            context.Error(DiagnosticCodes.InvalidSpecificationDenied, "Expected exactly 'then denied'.", line.Location);
                            SkipBody(context, line.Indent);
                        }
                        else if (denied is not null)
                        {
                            context.Error(DiagnosticCodes.DuplicateSpecificationCallerOrDenied, "A specification has at most one 'then denied' outcome.", line.Location);
                        }
                        else
                        {
                            denied = new(line.Location);
                        }
                    }
                    else
                    {
                        ParseThen(context, line, thenEvents, thenReadModels, thenAbsentReadModels, thenQueries, thenErrors);
                    }
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownSpecificationDirective, $"Unexpected '{LineText.FirstWord(line.Content)}' in specification body", line.Location);
                    SkipBody(context, line.Indent);
                    break;
            }
        }

        if (thenNoEvents && (whenAppended is not null || thenEvents.Count > 0 || eventsInAnyOrder || thenErrors.Count > 0 || denied is not null))
        {
            context.Error(
                DiagnosticCodes.InvalidNoEventsExpectation,
                "'then no events' cannot follow 'when append' or accompany event, event-order, error or denial expectations.",
                directiveLocations["then no events"]);
        }

        return new(name, given, when, thenEvents, thenErrors, header.Location, givenReadModels, thenReadModels)
        {
            SourceOptions = context.SourceOptions,
            File = file,
            ThenQueries = thenQueries,
            ThenAbsentReadModels = thenAbsentReadModels,
            GivenCaller = caller,
            ThenDenied = denied,
            ThenReturns = thenReturns,
            GivenOperationFailures = givenOperationFailures,
            ThenOperations = thenOperations,
            ThenCompensated = thenCompensated,
            WhenAppended = whenAppended,
            WhenRedelivered = whenRedelivered,
            ThenEventsInAnyOrder = eventsInAnyOrder,
            ThenNoEvents = thenNoEvents,
            GivenClock = givenClock,
            GivenCaptures = givenCaptures,
            WhenClock = whenClock,
            WhenTrigger = whenTrigger,
            WhenCapture = whenCapture,
            WhenQuery = whenQuery,
            ThenResults = thenResults,
            ThenNoResult = thenNoResult,
            DirectiveLocations = WithEventOrderLocation(directiveLocations, eventsInAnyOrderLocation)
        };
    }

    [GeneratedRegex(@"^then\s+no\s+events\b", RegexOptions.None, 1000)]
    private static partial Regex ThenNoEventsPrefixRegex();

    static Dictionary<string, SourceLocation> WithEventOrderLocation(Dictionary<string, SourceLocation> locations, SourceLocation? location)
    {
        if (location is not null)
        {
            locations["then events in any order"] = location;
        }

        return locations;
    }

    // Skipped bodies are not modeled; an Exact document steps over fenced code whole.
    static void SkipBody(ParserContext context, int parentIndent)
    {
        if (context.SourceOptions.NumericMode != NumericMode.Exact)
        {
            context.SkipBlock(parentIndent);
            return;
        }

        while (context.TryPeekChild(parentIndent, out var child))
        {
            context.Reader.TakeSignificant();
            if (child.Content.StartsWith("```", StringComparison.Ordinal))
            {
                while (context.Reader.TakeRaw() is { } raw && !CodeBlockParser.IsClosingFence(raw))
                {
                }
            }
        }
    }

    static SpecificationClockSyntax? ParseClock(ParserContext context, SourceLine line, string keyword)
    {
        var match = ClockRegex().Match(line.Content);
        SkipBody(context, line.Indent);
        if (!match.Success || match.Groups[1].Value != keyword)
        {
            context.Error(
                DiagnosticCodes.InvalidSpecificationClock,
                $"Invalid '{keyword} clock' - expected '{keyword} clock \"<ISO 8601 instant>\"', such as '{keyword} clock \"2026-10-05T08:00:00Z\"'",
                line.Location);
            return null;
        }

        return new(match.Groups[2].Value, line.Location);
    }

    static SpecificationRedeliverySyntax? ParseRedelivery(ParserContext context, SourceLine line)
    {
        var match = WhenRedeliveredRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.UnmatchedRedeliveredOccurrence, "Expected 'when redelivered <Event> to <Reaction>'.", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        var body = ParseValuesWithEventSource(context, line);
        return new(match.Groups[1].Value, match.Groups[2].Value, body.Values, line.Location) { For = body.For };
    }

    [GeneratedRegex(@"^when\s+redelivered\b", RegexOptions.None, 1000)]
    private static partial Regex WhenRedeliveredPrefixRegex();

    [GeneratedRegex(@"^when\s+redelivered\s+([A-Za-z_]\w*(?:\.\w+)*)\s+to\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex WhenRedeliveredRegex();

    static SpecificationTriggerSyntax? ParseTrigger(ParserContext context, SourceLine line)
    {
        var match = WhenTriggerRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationTrigger, $"Invalid 'when trigger' declaration '{line.Content}' - expected 'when trigger <Trigger>'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        return new(match.Groups[1].Value, ParseValues(context, line), line.Location);
    }

    static SpecificationCaptureSyntax? ParseCapture(ParserContext context, SourceLine line, string keyword)
    {
        var match = CaptureRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationCapture, $"Invalid '{keyword} capture' declaration '{line.Content}' - expected '{keyword} capture <Capture>'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        return new(match.Groups[1].Value, ParseValues(context, line), line.Location);
    }

    static SpecificationWhenQuerySyntax? ParseWhenQuery(ParserContext context, SourceLine line)
    {
        var match = WhenQueryRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationQueryAction, $"Invalid 'when query' declaration '{line.Content}' - expected 'when query <Query>'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        return new(match.Groups[1].Value, ParseValues(context, line), line.Location);
    }

    static SpecificationQueryResultSyntax? ParseResult(ParserContext context, SourceLine line)
    {
        var match = ThenResultRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationQueryAction, $"Invalid 'then result' declaration '{line.Content}' - expected 'then result [exactly]'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        return new(ParseValues(context, line), line.Location) { Exactly = match.Groups[1].Success };
    }

    static Regex KeywordRegex(string verb, string keyword) => verb switch
    {
        "given" => keyword == "clock" ? GivenClockPrefixRegex() : GivenCapturePrefixRegex(),
        "when" => keyword switch
        {
            "clock" => WhenClockPrefixRegex(),
            "trigger" => WhenTriggerPrefixRegex(),
            "capture" => WhenCapturePrefixRegex(),
            _ => WhenQueryPrefixRegex()
        },
        _ => ThenResultPrefixRegex()
    };

    static SpecificationCallerSyntax? ParseCaller(ParserContext context, SourceLine line)
    {
        if (line.Content != "given caller")
        {
            context.Error(DiagnosticCodes.InvalidSpecificationCaller, "Expected exactly 'given caller'.", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        var authenticated = false;
        var seenAuthenticated = false;
        var roles = new List<string>();
        var claims = new List<SpecificationCallerClaimSyntax>();
        var directiveLocations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (child.Content == "authenticated" && !seenAuthenticated)
            {
                authenticated = seenAuthenticated = true;
                directiveLocations["authenticated"] = child.Location;
                continue;
            }

            var role = CallerRoleRegex().Match(child.Content);
            if (role.Success)
            {
                roles.Add(StringLiteral.Unescape(role.Groups[1].Value));
                directiveLocations[DirectiveLocationKeys.ForValue("role", roles, roles.Count - 1)] = child.Location;
                continue;
            }

            var claim = CallerClaimRegex().Match(child.Content);
            if (claim.Success)
            {
                claims.Add(new(StringLiteral.Unescape(claim.Groups[1].Value), StringLiteral.Unescape(claim.Groups[2].Value), child.Location));
                continue;
            }

            context.Error(DiagnosticCodes.InvalidSpecificationCaller, $"Invalid caller fixture '{child.Content}' - expected authenticated, role \"...\", or claim \"...\" = \"...\".", child.Location);
        }

        return new(authenticated, roles, claims, line.Location) { DirectiveLocations = directiveLocations };
    }

    static SpecificationCommandSyntax? ParseWhen(ParserContext context, SourceLine line)
    {
        var match = WhenRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationWhen, $"Invalid 'when' declaration '{line.Content}' - expected 'when <CommandType>' or 'when append <EventType>'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        var generated = new List<PropertyMappingSyntax>();
        var body = ParseValuesWithEventSource(context, line, generated, match);
        return new SpecificationCommandSyntax(match.Groups[1].Value, body.Values, line.Location)
        {
            For = body.For,
            GeneratedValues = generated,
            InlineProperty = match.Groups["property"].Success ? match.Groups["property"].Value : null
        };
    }

    static void ParseThen(
        ParserContext context,
        SourceLine line,
        List<SpecificationEventSyntax> thenEvents,
        List<SpecificationReadModelSyntax> thenReadModels,
        List<SpecificationAbsentReadModelSyntax> thenAbsentReadModels,
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
            SkipBody(context, line.Indent);
            return;
        }

        if (ThenNoPrefixRegex().IsMatch(line.Content))
        {
            if (ParseAbsentReadModel(context, line) is { } absent)
            {
                thenAbsentReadModels.Add(absent);
            }

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

    static SpecificationAbsentReadModelSyntax? ParseAbsentReadModel(ParserContext context, SourceLine line)
    {
        var match = ThenAbsentReadModelRegex().Match(line.Content);
        if (!match.Success || match.Groups[2].Value.EndsWith(" exactly", StringComparison.Ordinal))
        {
            context.Error(DiagnosticCodes.InvalidAbsentReadModelStep, $"Invalid absence assertion '{line.Content}' - expected 'then no readmodel <ReadModelType> for <key>'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        var keyGroup = match.Groups[2];
        var keyText = keyGroup.Value.Trim();
        var keyStart = line.LocationAt(keyGroup.Index + (keyGroup.Value.Length - keyGroup.Value.TrimStart().Length));
        var validString = !(keyText.StartsWith('"') || keyText.StartsWith('\'')) || AbsentKeyStringRegex().IsMatch(keyText);
        var key = validString ? ExpressionParser.ParseMappingSource(context, keyText, keyText.StartsWith('{') || keyText.StartsWith('[') ? keyStart : line.Location) : null;
        if (key is LiteralExpressionSyntax literal)
        {
            key = literal with { RawLocation = keyStart, RawLength = keyText.Length };
        }

        if (key is not LiteralExpressionSyntax and not ObjectExpressionSyntax)
        {
            context.Error(DiagnosticCodes.InvalidAbsentReadModelStep, $"Invalid absence key '{keyText}' - expected exactly one concrete value.", line.Location);
        }

        var hasChildren = false;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            context.Error(DiagnosticCodes.InvalidAbsentReadModelStep, "An absent read model assertion cannot have child mappings.", child.Location);
            hasChildren = true;
        }

        return hasChildren || key is not LiteralExpressionSyntax and not ObjectExpressionSyntax ? null : new(match.Groups[1].Value, key, line.Location);
    }

    static SpecificationQuerySyntax? ParseQuery(ParserContext context, SourceLine line)
    {
        var match = ThenQueryRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationQuery, $"Invalid 'then query' declaration '{line.Content}' - expected 'then query <Query> [exactly]'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        var arguments = new List<PropertyMappingSyntax>();
        var results = new List<SpecificationQueryResultSyntax>();
        SourceLocation? argumentsLocation = null;
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
                    argumentsLocation = child.Location;
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

        return new(match.Groups[1].Value, arguments, results, line.Location)
        {
            Exactly = match.Groups[2].Success,
            DirectiveLocations = argumentsLocation is null ? [] : new Dictionary<string, SourceLocation> { ["arguments"] = argumentsLocation }
        };
    }

    static SpecificationReadModelSyntax? ParseReadModel(ParserContext context, SourceLine line, Regex regex, string keyword)
    {
        var match = regex.Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidReadModelStep, $"Invalid '{keyword} readmodel' declaration '{line.Content}' - expected '{keyword} readmodel <ReadModelType>'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        return new(match.Groups[1].Value, ParseValues(context, line, match), line.Location)
        {
            Exactly = keyword == "then" && match.Groups["exactly"].Success,
            InlineProperty = match.Groups["property"].Success ? match.Groups["property"].Value : null
        };
    }

    static SpecificationEventSyntax? ParseEventReference(ParserContext context, SourceLine line, Regex regex, string keyword)
    {
        var match = regex.Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidEventStep, $"Invalid '{keyword}' declaration '{line.Content}' - expected '{keyword} <EventType>'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        var body = ParseValuesWithEventSource(context, line, inline: match);
        return new SpecificationEventSyntax(match.Groups[1].Value, body.Values, line.Location)
        {
            For = body.For,
            InlineProperty = match.Groups["property"].Success ? match.Groups["property"].Value : null
        };
    }

    static (List<PropertyMappingSyntax> Values, ExpressionSyntax? For) ParseValuesWithEventSource(
        ParserContext context,
        SourceLine parent,
        List<PropertyMappingSyntax>? generated = null,
        Match? inline = null)
    {
        var body = ParseFixtureBody(context, parent, inline, allowGenerated: generated is not null);
        generated?.AddRange(body.Generated);

        return (body.Values, body.For);
    }

    static List<PropertyMappingSyntax> ParseValues(ParserContext context, SourceLine parent, Match? inline = null)
    {
        var values = new List<PropertyMappingSyntax>();
        AddInlineValue(context, parent, inline, values);
        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var match = MappingRegex().Match(child.Content);
            if (!match.Success)
            {
                context.Error(DiagnosticCodes.InvalidSpecificationValue, $"Invalid property mapping '{child.Content}' - expected '<property> = <value>'", child.Location);
                continue;
            }

            AddFixtureValue(context, values, ExpressionParser.ParseMapping(context, match.Groups[1].Value, match.Groups[2], child));
        }

        return values;
    }

    [GeneratedRegex("^role\\s+\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.None, 1000)]
    private static partial Regex CallerRoleRegex();

    [GeneratedRegex("^claim\\s+\"(" + StringLiteral.BodyPattern + ")\"\\s*=\\s*\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.None, 1000)]
    private static partial Regex CallerClaimRegex();

    [GeneratedRegex(@"^given\s+clock\b", RegexOptions.None, 1000)]
    private static partial Regex GivenClockPrefixRegex();

    [GeneratedRegex(@"^given\s+capture\b", RegexOptions.None, 1000)]
    private static partial Regex GivenCapturePrefixRegex();

    [GeneratedRegex(@"^when\s+clock\b", RegexOptions.None, 1000)]
    private static partial Regex WhenClockPrefixRegex();

    [GeneratedRegex(@"^when\s+trigger\b", RegexOptions.None, 1000)]
    private static partial Regex WhenTriggerPrefixRegex();

    [GeneratedRegex(@"^when\s+capture\b", RegexOptions.None, 1000)]
    private static partial Regex WhenCapturePrefixRegex();

    [GeneratedRegex(@"^when\s+query\b", RegexOptions.None, 1000)]
    private static partial Regex WhenQueryPrefixRegex();

    [GeneratedRegex(@"^then\s+result\b", RegexOptions.None, 1000)]
    private static partial Regex ThenResultPrefixRegex();

    [GeneratedRegex(@"^then\s+no\s+result\b", RegexOptions.None, 1000)]
    private static partial Regex NoResultRegex();

    // An instant in ISO 8601 - a date, a time to the minute or finer, and an explicit offset or Z - so the
    // same text means the same moment wherever the specification runs.
    [GeneratedRegex(@"^(given|when)\s+clock\s+""(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(?::\d{2}(?:\.\d+)?)?(?:Z|[+-]\d{2}:\d{2}))""$", RegexOptions.None, 1000)]
    private static partial Regex ClockRegex();

    [GeneratedRegex(@"^when\s+trigger\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex WhenTriggerRegex();

    [GeneratedRegex(@"^(?:given|when)\s+capture\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex CaptureRegex();

    [GeneratedRegex(@"^when\s+query\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex WhenQueryRegex();

    [GeneratedRegex(@"^then\s+result(\s+exactly)?$", RegexOptions.None, 1000)]
    private static partial Regex ThenResultRegex();

    [GeneratedRegex(@"^specification\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^given\s+([A-Z]\w*(?:\.\w+)*)(?:\s+(?<property>[\w.]+)\s*=(?!=|>)\s*(?<value>.+))?$", RegexOptions.None, 1000)]
    private static partial Regex GivenRegex();

    [GeneratedRegex(@"^when\s+append\s+([A-Z]\w*(?:\.\w+)*)(?:\s+(?<property>[\w.]+)\s*=(?!=|>)\s*(?<value>.+))?$", RegexOptions.None, 1000)]
    private static partial Regex WhenAppendRegex();

    [GeneratedRegex(@"^when\s+([A-Z]\w*(?:\.\w+)*)(?:\s+(?<property>[\w.]+)\s*=(?!=|>)\s*(?<value>.+))?$", RegexOptions.None, 1000)]
    private static partial Regex WhenRegex();

    [GeneratedRegex(@"^then\s+([A-Z]\w*(?:\.\w+)*)(?:\s+(?<property>[\w.]+)\s*=(?!=|>)\s*(?<value>.+))?$", RegexOptions.None, 1000)]
    private static partial Regex ThenEventRegex();

    [GeneratedRegex(@"^then\s+query\b", RegexOptions.None, 1000)]
    private static partial Regex ThenQueryPrefixRegex();

    [GeneratedRegex(@"^then\s+query\s+([A-Za-z_]\w*(?:\.\w+)*)(\s+exactly)?$", RegexOptions.None, 1000)]
    private static partial Regex ThenQueryRegex();

    [GeneratedRegex(@"^(?:given|then)\s+readmodel\b", RegexOptions.None, 1000)]
    private static partial Regex ReadModelPrefixRegex();

    [GeneratedRegex(@"^given\s+readmodel\s+([A-Z]\w*(?:\.\w+)*)(?:\s+(?<property>[\w.]+)\s*=(?!=|>)\s*(?<value>.+))?$", RegexOptions.None, 1000)]
    private static partial Regex GivenReadModelRegex();

    [GeneratedRegex(@"^then\s+readmodel\s+([A-Z]\w*(?:\.\w+)*)(?<exactly>\s+exactly)?(?:\s+(?<property>[\w.]+)\s*=(?!=|>)\s*(?<value>.+))?$", RegexOptions.None, 1000)]
    private static partial Regex ThenReadModelRegex();

    [GeneratedRegex(@"^then\s+no\b", RegexOptions.None, 1000)]
    private static partial Regex ThenNoPrefixRegex();

    [GeneratedRegex(@"^then\s+no\s+readmodel\s+([A-Z]\w*)\s+for\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex ThenAbsentReadModelRegex();

    [GeneratedRegex("^(?:\"" + StringLiteral.BodyPattern + "\"|'(?:[^'\\\\]|\\\\.)*')$", RegexOptions.None, 1000)]
    private static partial Regex AbsentKeyStringRegex();

    [GeneratedRegex("^then\\s+error\\s+\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.None, 1000)]
    private static partial Regex ThenErrorRegex();

    [GeneratedRegex(@"^([\w.]+)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();
}

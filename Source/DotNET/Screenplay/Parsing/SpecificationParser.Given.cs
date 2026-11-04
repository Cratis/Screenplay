// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static partial class SpecificationParser
{
    /// <summary>
    /// The Given rules in the order they are tried: the first one that matches handles the line, and the
    /// event rule matches everything so malformed Given input keeps the event parser's diagnostics and recovery.
    /// </summary>
    static readonly GivenRule[] _givenRules =
    [
        new(line => KeywordRegex("given", "clock").IsMatch(line.Content), ParseGivenClock),
        new(line => GivenTimeZonePrefixRegex().IsMatch(line.Content), ParseGivenTimeZone),
        new(line => GivenGeneratedPrefixRegex().IsMatch(line.Content), ParseGivenGeneratedValue),
        new(line => KeywordRegex("given", "capture").IsMatch(line.Content), ParseGivenCapture),
        new(line => line.Content.StartsWith("given caller", StringComparison.Ordinal), ParseGivenCaller),
        new(line => ReadModelPrefixRegex().IsMatch(line.Content), ParseGivenReadModel),
        new(_ => true, ParseGivenEvent)
    ];

    static void DispatchGiven(ParserContext context, SourceLine line, GivenBodyState state)
    {
        foreach (var rule in _givenRules)
        {
            if (rule.Matches(line))
            {
                rule.Parse(context, line, state);
                return;
            }
        }
    }

    static void ParseGivenClock(ParserContext context, SourceLine line, GivenBodyState state)
    {
        if (state.Clock is not null)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationClock, "A specification states its clock at most once.", line.Location);
            SkipBody(context, line.Indent);
            return;
        }

        state.Clock = ParseClock(context, line, "given");
    }

    static void ParseGivenTimeZone(ParserContext context, SourceLine line, GivenBodyState state)
    {
        var parsedZone = ParseTimeZone(context, line);
        if (state.TimeZone is not null || parsedZone is null)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationTimeZone, "A specification states its time zone at most once, as 'given time zone \"<id>\"'.", line.Location);
            return;
        }

        state.TimeZone = parsedZone;
    }

    static void ParseGivenGeneratedValue(ParserContext context, SourceLine line, GivenBodyState state)
    {
        if (ParseGivenGenerated(context, line) is { } pinned)
        {
            state.Generated.Add(pinned);
        }
    }

    static void ParseGivenCapture(ParserContext context, SourceLine line, GivenBodyState state)
    {
        if (ParseCapture(context, line, "given") is { } capture)
        {
            state.Captures.Add(capture);
        }
    }

    static void ParseGivenCaller(ParserContext context, SourceLine line, GivenBodyState state)
    {
        if (state.Caller is not null || state.CallerPersona is not null)
        {
            context.Error(DiagnosticCodes.DuplicateSpecificationCallerOrDenied, "A specification has at most one 'given caller' block.", line.Location);
            SkipBody(context, line.Indent);
        }
        else if (line.Content.StartsWith("given caller as", StringComparison.Ordinal))
        {
            state.CallerPersona = ParseCallerPersona(context, line);
        }
        else
        {
            state.Caller = ParseCaller(context, line);
        }
    }

    static void ParseGivenReadModel(ParserContext context, SourceLine line, GivenBodyState state)
    {
        if (ParseReadModel(context, line, GivenReadModelRegex(), "given") is { } readModel)
        {
            state.ReadModels.Add(readModel);
        }
    }

    static void ParseGivenEvent(ParserContext context, SourceLine line, GivenBodyState state)
    {
        if (ParseEventReference(context, line, GivenRegex(), "given") is { } givenEvent)
        {
            state.Events.Add(givenEvent);
        }
    }

    sealed record GivenRule(Func<SourceLine, bool> Matches, Action<ParserContext, SourceLine, GivenBodyState> Parse);

    sealed class GivenBodyState
    {
        public SpecificationClockSyntax? Clock { get; set; }

        public SpecificationTimeZoneSyntax? TimeZone { get; set; }

        public SpecificationCallerSyntax? Caller { get; set; }

        public SpecificationCallerPersonaSyntax? CallerPersona { get; set; }

        public List<SpecificationEventSyntax> Events { get; } = [];

        public List<SpecificationReadModelSyntax> ReadModels { get; } = [];

        public List<SpecificationCaptureSyntax> Captures { get; } = [];

        public List<SpecificationGeneratedValueSyntax> Generated { get; } = [];
    }
}

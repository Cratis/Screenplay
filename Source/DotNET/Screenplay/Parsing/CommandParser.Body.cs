// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class CommandParser
{
    /// <summary>
    /// The command body rules in the order they are tried: the first one whose keyword matches and whose guard holds
    /// handles the line, so a keyword that has a property shaped rule keeps it ahead of its directive rule. A line no
    /// rule takes is a property, or an unexpected directive.
    /// </summary>
    static readonly CommandDirectiveRule[] _directiveRules =
    [
        new(["runs"], (line, _) => ReactionIdentityParser.IsDeclarationLine(line), (context, line, _) => ReactionIdentityParser.Misplaced(context, line)),

        // The bare directives below cannot take a type reference, so a line that has property shape
        // is a property no matter which keyword it starts with - 'description String' declares a
        // property called description. Only the directives that do take an identifier operand
        // ('authorize', 'produces') stay ambiguous, and those use the '@' escape.
        new(["calendar"], (line, _) => CommandCalendarParser.IsDeclaration(line.Content), ParseCalendarDirective),
        new(["description", "documentation", "handler", "concurrency"], (line, _) => PropertyLineParser.TryParse(line) is not null, AddPropertyDirective),
        new(["validate"], (line, _) => line.Content != "validate csharp" && PropertyLineParser.TryParse(line) is not null, AddPropertyDirective),
        new(["returns"], null, ParseReturnsDirective),
        new(["stream"], IsKnownStreamRoute, ParseStreamDirective),
        new(["description"], null, ParseDescriptionDirective),
        new(["documentation"], null, ParseDocumentationDirective),
        new(["authorize"], null, ParseAuthorizeDirective),
        new(["concurrency"], null, (context, line, state) => state.Concurrency = ParseConcurrency(context, line, state.Concurrency, state.CommandName)),
        new(["validate"], null, ParseValidateDirective),
        new(["produces"], null, ParseProducesDirective),
        new(["reads"], null, ParseReadsDirective),
        new(["handler"], null, ParseHandlerDirective)
    ];

    static void DispatchDirective(ParserContext context, SourceLine line, CommandBodyState state)
    {
        var keyword = LineText.FirstWord(line.Content);
        foreach (var rule in _directiveRules)
        {
            if (rule.Matches(keyword, line, state))
            {
                rule.Parse(context, line, state);
                return;
            }
        }

        ParsePropertyOrUnexpected(context, line, state);
    }

    static bool IsKnownStreamRoute(SourceLine line, CommandBodyState state) =>
        RouteRegex().IsMatch(line.Content) &&
        PropertyLineParser.TryParse(line) is { } route &&
        state.StreamCandidates?.HasSource(route.Type.Name.Split('.')[0]) == true;

    static void AddPropertyDirective(ParserContext context, SourceLine line, CommandBodyState state) =>
        AddProperty(context, state.Properties, PropertyLineParser.TryParse(line)!, state.CommandName, line);

    static void ParseCalendarDirective(ParserContext context, SourceLine line, CommandBodyState state)
    {
        var parsedCalendar = CommandCalendarParser.Parse(line);
        if (state.Calendar is not null || parsedCalendar is null)
        {
            context.Error(DiagnosticCodes.InvalidCommandCalendar, "A command declares at most one 'calendar \"<id>\"' or 'calendar from $context.<member>'.", line.Location);
        }
        else
        {
            state.Calendar = parsedCalendar;
        }
    }

    static void ParseReturnsDirective(ParserContext context, SourceLine line, CommandBodyState state)
    {
        if (ScalarResponseRegex().Match(line.Content) is { Success: true } scalar && !scalar.Groups[1].Value.StartsWith('@') && PropertyLineParser.TryParse(line) is { } candidate)
        {
            ParseScalarReturns(context, line, state, candidate);
        }
        else if (PropertyLineParser.TryParse(line) is { } returnsProperty)
        {
            AddProperty(context, state.Properties, returnsProperty, state.CommandName, line);
        }
        else if (PropertyLineParser.ReportInvalidModifierOrder(context, line))
        {
            context.SkipBlock(line.Indent);
        }
        else
        {
            state.Responses.Add((line, null, ParseResponse(context, line)));
        }
    }

    static void ParseScalarReturns(ParserContext context, SourceLine line, CommandBodyState state, PropertySyntax candidate)
    {
        if (state.ResponseNames is null)
        {
            state.Properties.Add(candidate);
            state.Responses.Add((line, candidate, null));
        }
        else if (state.ResponseNames.Contains(candidate.Type.Name))
        {
            state.Responses.Add((line, null, ParseResponse(context, line)));
        }
        else
        {
            AddProperty(context, state.Properties, candidate, state.CommandName, line);
        }
    }

    static void ParseStreamDirective(ParserContext context, SourceLine line, CommandBodyState state)
    {
        var streamProperty = PropertyLineParser.TryParse(line)!;
        var segments = streamProperty.Type.Name.Split('.');
        var ambiguous = state.StreamCandidates!.HasPropertyType(streamProperty.Type.Name) && state.StreamCandidates.HasUniqueStream(segments[0], segments[1]);
        var route = EventSourceParser.ParseRoute(context, line, streamProperty, ambiguous);
        if (state.Stream is not null || state.StreamRoutes.Count > 0)
        {
            context.Error(DiagnosticCodes.InvalidCommandStream, "A command declares at most one stream route.", line.Location);
        }

        if (ambiguous || state.Stream is not null)
        {
            state.StreamRoutes.Add(route);
        }
        else
        {
            state.Stream = route;
        }
    }

    static void ParseDescriptionDirective(ParserContext context, SourceLine line, CommandBodyState state)
    {
        var previousDescription = state.Description;
        state.Description = DescriptionParser.Parse(context, line, state.Description, $"Command '{state.CommandName}'");
        if (previousDescription is null && state.Description is not null)
        {
            state.DirectiveLocations["description"] = line.Location;
        }
    }

    static void ParseDocumentationDirective(ParserContext context, SourceLine line, CommandBodyState state) =>
        state.Documentation = DocumentationParser.Parse(context, line, state.Documentation, $"Command '{state.CommandName}'", state.DirectiveLocations);

    static void ParseAuthorizeDirective(ParserContext context, SourceLine line, CommandBodyState state)
    {
        var next = AuthorizeParser.Parse(context, line);
        if (state.AuthorizationDepthExceeded)
        {
            return;
        }

        var previous = state.Authorize;
        state.Authorize = AuthorizeParser.Combine(state.Authorize, next, context);
        state.AuthorizationDepthExceeded = previous is not null && state.Authorize is null;
    }

    static void ParseValidateDirective(ParserContext context, SourceLine line, CommandBodyState state)
    {
        if (ValidateParser.Parse(context, line, ValidationOwnerKind.Command) is { } validate)
        {
            state.Validations.Add(validate);
        }
    }

    static void ParseProducesDirective(ParserContext context, SourceLine line, CommandBodyState state)
    {
        if (ProducesParser.Parse(context, line, true) is { } production)
        {
            state.Produces.Add(production);
        }
    }

    static void ParseReadsDirective(ParserContext context, SourceLine line, CommandBodyState state)
    {
        if (ReadsParser.Parse(context, line) is { } read)
        {
            state.Reads.Add(read);
        }
    }

    static void ParseHandlerDirective(ParserContext context, SourceLine line, CommandBodyState state)
    {
        var parsedHandler = ParseHandler(context, line);
        state.Handlers.Add((parsedHandler, line.Location));
        if (parsedHandler is not null && state.Handler is not null)
        {
            // Duplicate handlers are diagnosed below. Keep the replaced source locations
            // for recovery printing, so their comments stay beside the retained handler
            // instead of merging into the command header.
            RecordReplacedHandler(state.DirectiveLocations, state.Handler);
        }

        state.Handler = parsedHandler;
    }

    static void RecordReplacedHandler(Dictionary<string, SourceLocation> directiveLocations, HandlerSyntax replaced)
    {
        directiveLocations[$"omitted:handler:{replaced.Location.Line}"] = replaced.Location;
        if (replaced.File is { } replacedFile)
        {
            directiveLocations[$"omitted:handler:{replacedFile.Location.Line}"] = replacedFile.Location;
        }

        if (replaced.Code is { } replacedCode)
        {
            directiveLocations[$"omitted:handler:{replacedCode.Location.Line}"] = replacedCode.Location;
        }
    }

    static void ParsePropertyOrUnexpected(ParserContext context, SourceLine line, CommandBodyState state)
    {
        if (PropertyLineParser.TryParse(line) is { } property)
        {
            AddProperty(context, state.Properties, property, state.CommandName, line);
            return;
        }

        PropertyLineParser.ReportInvalidModifierOrder(context, line);
        context.Error(DiagnosticCodes.UnknownCommandDirective, $"Unexpected '{line.Content}' in command body{MisplacedRuleHint.ForCommand(line.Content)}", line.Location);
        context.SkipBlock(line.Indent);
    }

    /// <summary>
    /// A command body rule: the keywords that open it, an optional guard that must also hold, and what to do with the line.
    /// </summary>
    /// <param name="Keywords">The first words of the lines the rule takes.</param>
    /// <param name="When">An extra condition on the line and the state, or <see langword="null"/> when the keyword alone decides.</param>
    /// <param name="Parse">Handles the line.</param>
    sealed record CommandDirectiveRule(string[] Keywords, Func<SourceLine, CommandBodyState, bool>? When, Action<ParserContext, SourceLine, CommandBodyState> Parse)
    {
        public bool Matches(string keyword, SourceLine line, CommandBodyState state) =>
            Array.IndexOf(Keywords, keyword) >= 0 && (When?.Invoke(line, state) ?? true);
    }

    /// <summary>
    /// Everything the command body loop gathers while it reads the directives of one command.
    /// </summary>
    /// <param name="commandName">The name of the command, used in diagnostics.</param>
    /// <param name="responseNames">The property names already known to be response sources, or <see langword="null"/> for the discovery pass.</param>
    /// <param name="streamCandidates">The source streams a stream route can name.</param>
    sealed class CommandBodyState(string commandName, HashSet<string>? responseNames, CommandStreamCandidates? streamCandidates)
    {
        public string CommandName { get; } = commandName;

        public HashSet<string>? ResponseNames { get; } = responseNames;

        public CommandStreamCandidates? StreamCandidates { get; } = streamCandidates;

        public List<PropertySyntax> Properties { get; } = [];

        public List<(SourceLine Line, PropertySyntax? Candidate, CommandResponseSyntax? Response)> Responses { get; } = [];

        public AuthorizeSyntax? Authorize { get; set; }

        public bool AuthorizationDepthExceeded { get; set; }

        public List<ValidateSyntax> Validations { get; } = [];

        public List<ProducesSyntax> Produces { get; } = [];

        public List<ReadsSyntax> Reads { get; } = [];

        public HandlerSyntax? Handler { get; set; }

        public List<(HandlerSyntax? Handler, SourceLocation Location)> Handlers { get; } = [];

        public ConcurrencySyntax? Concurrency { get; set; }

        public CommandStreamSyntax? Stream { get; set; }

        public CommandCalendarSyntax? Calendar { get; set; }

        public List<CommandStreamSyntax> StreamRoutes { get; } = [];

        public string? Description { get; set; }

        public string? Documentation { get; set; }

        public Dictionary<string, SourceLocation> DirectiveLocations { get; } = [];
    }
}

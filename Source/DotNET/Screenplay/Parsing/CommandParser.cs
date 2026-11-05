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
        // Ordinary properties are leaves even when later members have a greater indent.
        // Resolve ambiguous scalar spelling using a noncommitting command-body pass first.
        var names = new HashSet<string>(StringComparer.Ordinal);
        ParseBody(new(context.Reader.Fork(), context.Start.Path, context.Languages) { SourceOptions = context.SourceOptions, StreamCandidates = context.StreamCandidates }, header, null, names);
        return ParseBody(context, header, names, null);
    }

    static CommandSyntax ParseBody(ParserContext context, SourceLine header, HashSet<string>? responseNames, HashSet<string>? discoveredNames)
    {
        var name = HeaderRegex().Match(header.Content);
        if (!name.Success)
        {
            context.Error(DiagnosticCodes.InvalidCommandDeclaration, $"Invalid command declaration '{header.Content}' - expected 'command <Name>'", header.Location);
        }

        var state = new CommandBodyState(name.Groups[1].Value, responseNames, context.StreamCandidates);
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            DispatchDirective(context, line, state);
        }

        return BuildCommand(context, header, state, discoveredNames);
    }

    static CommandSyntax BuildCommand(ParserContext context, SourceLine header, CommandBodyState state, HashSet<string>? discoveredNames)
    {
        var (response, properties) = ResolveResponse(context, state, discoveredNames);
        CommandHandlerValidator.Validate(state.Handlers, state.Handler, state.Produces.Count > 0, context);

        return new(state.CommandName, properties, state.Authorize, state.Validations, state.Produces, state.Handler, header.Location, state.Concurrency, state.Description, state.Reads)
        {
            Documentation = state.Documentation,
            DirectiveLocations = state.DirectiveLocations,
            Response = response,
            Stream = state.Stream,
            Calendar = state.Calendar,
            StreamCandidates = state.StreamRoutes
        };
    }

    static (CommandResponseSyntax? Response, List<PropertySyntax> Properties) ResolveResponse(ParserContext context, CommandBodyState state, HashSet<string>? discoveredNames)
    {
        var responses = state.Responses;
        var candidates = responses.Where(entry => entry.Candidate is not null).Select(entry => entry.Candidate).ToHashSet();
        var names = state.Properties.Where(property => !candidates.Contains(property)).Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        if (responses.Exists(entry => entry.Candidate is { } candidate && !names.Contains(candidate.Type.Name) && candidate.Type.Name != "returns"))
        {
            names.Add("returns");
        }

        discoveredNames?.UnionWith(names);
        CommandResponseSyntax? response = null;
        var removed = new HashSet<PropertySyntax>();
        foreach (var entry in responses)
        {
            var parsed = entry.Response;
            if (entry.Candidate is { } candidate)
            {
                if (!names.Contains(candidate.Type.Name)) continue;
                removed.Add(candidate);
                parsed = new ScalarCommandResponseSyntax(new(candidate.Type.Name, candidate.Type.Location), entry.Line.Location);
            }

            if (parsed is null) continue;
            if (response is not null)
            {
                context.Error(DiagnosticCodes.InvalidCommandResponse, "A command declares at most one unconditional response.", entry.Line.Location);
            }
            else
            {
                response = parsed;
            }
        }

        return (response, [.. state.Properties.Where(property => !removed.Contains(property))]);
    }

    /// <summary>
    /// Adds a property to the command, keeping at most one of them marked as the identifier.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="properties">The properties parsed so far.</param>
    /// <param name="property">The <see cref="PropertySyntax"/> to add.</param>
    /// <param name="commandName">The name of the command, used in diagnostics.</param>
    /// <param name="line">The committed property line.</param>
    /// <remarks>
    /// The identifier is what a runtime resolves the event source id from, so a second one would leave it
    /// with no way to choose. The first declaration wins and the rest are reported.
    /// </remarks>
    static void AddProperty(ParserContext context, List<PropertySyntax> properties, PropertySyntax property, string commandName, SourceLine line)
    {
        PropertyLineParser.ReportLegacyOptionalSuffix(context, property.Type, line);
        property = PropertyLineParser.WithoutKey(context, property);
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
        var directiveLocations = new Dictionary<string, SourceLocation>();

        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "eventSource":
                    eventSource = ParseEventSourceDimension(context, child, eventSource);
                    if (eventSource)
                    {
                        directiveLocations.TryAdd("eventSource", child.Location);
                    }
                    break;
                case "sourceType":
                    eventSourceType = ParseNamedDimension(context, child, "sourceType", eventSourceType);
                    if (eventSourceType is not null)
                    {
                        directiveLocations.TryAdd("sourceType", child.Location);
                    }
                    break;
                case "streamType":
                    eventStreamType = ParseNamedDimension(context, child, "streamType", eventStreamType);
                    if (eventStreamType is not null)
                    {
                        directiveLocations.TryAdd("streamType", child.Location);
                    }
                    break;
                case "streamId":
                    eventStreamId = ParseNamedDimension(context, child, "streamId", eventStreamId);
                    if (eventStreamId is not null)
                    {
                        directiveLocations.TryAdd("streamId", child.Location);
                    }
                    break;
                case "events":
                    eventTypes = ParseEventsDimension(context, child, eventTypes);
                    if (eventTypes is { Count: > 0 })
                    {
                        directiveLocations.TryAdd("events", child.Location);
                    }
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownConcurrencyDimension, $"Unexpected '{child.Content}' in concurrency block - expected eventSource, sourceType, streamType, streamId or events", child.Location);
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(eventSource, eventSourceType, eventStreamType, eventStreamId, eventTypes ?? [], line.Location) { DirectiveLocations = directiveLocations };
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

    static HandlerSyntax? ParseHandler(ParserContext context, SourceLine line)
    {
        PossibleEventsSyntax? possible = null;
        if (context.TryPeekChild(line.Indent, out var first) && first.Content == "may produce")
        {
            context.Reader.TakeSignificant();
            possible = PossibleEventsParser.Parse(context, first);
        }

        var handler = ParseHandlerBody(context, line);
        return handler is null ? null : handler with { PossibleEvents = possible };
    }

    static HandlerSyntax? ParseHandlerBody(ParserContext context, SourceLine line)
    {
        if (!context.TryPeekChild(line.Indent, out var body))
        {
            context.Error(DiagnosticCodes.HandlerWithoutImplementation, "Expected a 'file' directive or an inline code block in the handler", line.Location);
            return null;
        }

        context.Reader.TakeSignificant();
        if (LineText.FirstWord(body.Content) == "implementation")
        {
            return ImplementationParser.Parse(context, line, body);
        }

        HandlerSyntax? result = null;
        if (FileReferenceParser.IsDirective(body))
        {
            result = new(FileReferenceParser.Parse(context, body), null, line.Location);
        }
        else if (CodeBlockParser.IsCodeLine(context, body))
        {
            var code = CodeBlockParser.Parse(context, body);
            result = code is null ? null : new HandlerSyntax(null, code, line.Location);
        }

        if (result is not null)
        {
            if (context.TryPeekChild(line.Indent, out var extra) && LineText.FirstWord(extra.Content) == "implementation")
            {
                context.Error(DiagnosticCodes.ConflictingImplementationSources, "A handler cannot mix wrapped and direct sources.", extra.Location);
                context.SkipBlock(line.Indent);
            }

            return result;
        }

        context.Error(DiagnosticCodes.UnknownHandlerDirective, $"Unexpected '{body.Content}' in handler - expected 'file <path>' or an inline code block", body.Location);
        context.SkipBlock(line.Indent);
        return null;
    }

    [GeneratedRegex(@"^stream\s+[A-Za-z_]\w*\.[A-Za-z_]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex RouteRegex();

    [GeneratedRegex(@"^command\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^(sourceType|streamType|streamId)\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ConcurrencyDimensionRegex();

    [GeneratedRegex(@"^([A-Z]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex EventNameRegex();
}

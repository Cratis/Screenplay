// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class EventSourceParser
{
    internal static EventSourceSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = SourceHeaderRegex().Match(header.Content);
        if (!match.Success) Invalid(context, header, "Expected 'eventsource <Name>'.");
        var name = match.Groups[1].Value;
        var streams = new List<EventStreamSyntax>();
        TypeRefSyntax? identifier = null;
        string? description = null;
        string? id = null;
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "stream":
                    streams.Add(ParseStream(context, child));
                    break;
                case "identifier":
                    identifier = ParseType(context, child, "identifier", identifier, locations);
                    break;
                case "description":
                    description = DescriptionParser.ParseEvent(context, child, description, $"Event source '{name}'");
                    locations.TryAdd("description", child.Location);
                    break;
                case "id":
                    id = ParsePin(context, child, name, id, locations);
                    break;
                default:
                    Invalid(context, child, "An event source accepts description, id, identifier and stream declarations.");
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(name, header.Location) { Identifier = identifier, Streams = streams, Description = description, Id = id, DirectiveLocations = locations };
    }

    internal static CommandStreamSyntax ParseRoute(ParserContext context, SourceLine header, PropertySyntax candidate, bool ambiguous)
    {
        var segments = candidate.Type.Name.Split('.');
        var route = new CommandStreamSyntax(segments[0], segments[1], header.Location)
        {
            ReferenceLocation = candidate.Type.Location,
            ReferenceLength = candidate.Type.Name.Length
        };
        if (ambiguous)
        {
            context.Error(DiagnosticCodes.AmbiguousCommandStream, $"'{header.Content}' has both a property type and a stream route interpretation; neither is selected.", header.Location);

            // Properties are leaves. In the ambiguous case do not consume or discard deeper legacy members.
            return route with { PropertyCandidate = candidate };
        }

        PropertyMappingSyntax? mapping = null;
        var parts = new List<PropertyMappingSyntax>();
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (child.Content == "streamId")
            {
                if (mapping is not null || locations.ContainsKey("streamId"))
                {
                    context.Error(DiagnosticCodes.InvalidCommandStream, "Declare either one 'streamId = <source>' mapping or one streamId part block.", child.Location);
                    context.SkipBlock(child.Indent);
                }
                else
                {
                    locations["streamId"] = child.Location;
                    parts = ParseRouteParts(context, child, DiagnosticCodes.InvalidCommandStream);
                }
                continue;
            }
            var match = MappingRegex().Match(child.Content);
            if (!match.Success || mapping is not null || locations.ContainsKey("streamId"))
            {
                context.Error(DiagnosticCodes.InvalidCommandStream, "Declare either one 'streamId = <source>' mapping or one streamId part block.", child.Location);
                context.SkipBlock(child.Indent);
            }
            else
            {
                mapping = ExpressionParser.ParseMapping(context, "streamId", match.Groups[1], child);
                RejectChildren(context, child, DiagnosticCodes.InvalidCommandStream);
            }
        }

        return route with { StreamId = mapping, StreamIdParts = parts, DirectiveLocations = locations };
    }

    internal static List<PropertyMappingSyntax> ParseRouteParts(ParserContext context, SourceLine header, string code)
    {
        var parts = new List<PropertyMappingSyntax>();
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var match = PartMappingRegex().Match(child.Content);
            if (!match.Success)
            {
                context.Error(code, "Expected '<part> = <source>' in a streamId part block.", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }
            parts.Add(ExpressionParser.ParseMapping(context, match.Groups[1].Value, match.Groups[2], child));
            RejectChildren(context, child, code);
        }
        if (parts.Count == 0) context.Error(code, "A streamId part block cannot be empty.", header.Location);

        return parts;
    }

    static EventStreamSyntax ParseStream(ParserContext context, SourceLine header)
    {
        var match = StreamHeaderRegex().Match(header.Content);
        if (!match.Success) Invalid(context, header, "Expected 'stream <Name>'.");
        var name = match.Groups[1].Value;
        TypeRefSyntax? streamId = null;
        var parts = new List<EventStreamIdPartSyntax>();
        string? description = null;
        string? id = null;
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "streamId":
                    if (locations.ContainsKey("streamId"))
                    {
                        Invalid(context, child, "Declare either one 'streamId <Type>' or one streamId part block.");
                        context.SkipBlock(child.Indent);
                    }
                    else if (child.Content == "streamId")
                    {
                        locations["streamId"] = child.Location;
                        parts = ParseStreamIdParts(context, child);
                    }
                    else
                    {
                        streamId = ParseType(context, child, "streamId", streamId, locations);
                    }
                    break;
                case "description":
                    description = DescriptionParser.ParseEvent(context, child, description, $"Stream '{name}'");
                    locations.TryAdd("description", child.Location);
                    break;
                case "id":
                    id = ParsePin(context, child, name, id, locations);
                    break;
                default:
                    Invalid(context, child, "A stream accepts description, id and streamId declarations.");
                    context.SkipBlock(child.Indent);
                    break;
            }
        }

        return new(name, header.Location) { StreamId = streamId, StreamIdParts = parts, Description = description, Id = id, DirectiveLocations = locations };
    }

    static List<EventStreamIdPartSyntax> ParseStreamIdParts(ParserContext context, SourceLine header)
    {
        var parts = new List<EventStreamIdPartSyntax>();
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var match = PartDeclarationRegex().Match(child.Content);
            if (!match.Success)
            {
                Invalid(context, child, "Expected '<part> <Type>' without modifiers in a streamId part block.");
                context.SkipBlock(child.Indent);
                continue;
            }
            var type = PropertyLineParser.ParseTypeRef(match.Groups[2].Value, child.LocationAt(match.Groups[2].Index));
            PropertyLineParser.ReportLegacyOptionalSuffix(context, type, child);
            parts.Add(new(match.Groups[1].Value, type, child.Location));
            RejectChildren(context, child, DiagnosticCodes.InvalidEventSourceDeclaration);
        }
        if (parts.Count == 0) Invalid(context, header, "A streamId part block cannot be empty.");

        return parts;
    }

    static TypeRefSyntax? ParseType(ParserContext context, SourceLine line, string keyword, TypeRefSyntax? previous, Dictionary<string, SourceLocation> locations)
    {
        var match = TypeDirectiveRegex().Match(line.Content);
        if (!match.Success || previous is not null)
        {
            Invalid(context, line, $"Declare at most one '{keyword} <Type>'.");
        }
        else
        {
            previous = PropertyLineParser.ParseTypeRef(match.Groups[2].Value, line.LocationAt(match.Groups[2].Index));
            PropertyLineParser.ReportLegacyOptionalSuffix(context, previous, line);
            locations[keyword] = line.Location;
        }
        RejectChildren(context, line, DiagnosticCodes.InvalidEventSourceDeclaration);
        return previous;
    }

    static string? ParsePin(ParserContext context, SourceLine line, string name, string? previous, Dictionary<string, SourceLocation> locations)
    {
        var match = PinRegex().Match(line.Content);
        var value = match.Success ? StringLiteral.Unescape(match.Groups[1].Value) : null;
        if (string.IsNullOrWhiteSpace(value) || previous is not null)
        {
            Invalid(context, line, "Declare at most one nonempty rename-only 'id \"<old-name>\"'.");
        }
        else
        {
            previous = value;
            locations["id"] = line.Location;
            if (value == name) context.Add(new(DiagnosticSeverity.Information, DiagnosticCodes.RedundantSourceStreamId, "The rename pin repeats the current name; omit it for a new declaration.", line.Location));
        }
        RejectChildren(context, line, DiagnosticCodes.InvalidEventSourceDeclaration);
        return previous;
    }

    static void RejectChildren(ParserContext context, SourceLine line, string code)
    {
        if (context.TryPeekChild(line.Indent, out var child))
        {
            context.Error(code, "This directive cannot have children.", child.Location);
            context.SkipBlock(line.Indent);
        }
    }

    static void Invalid(ParserContext context, SourceLine line, string message) => context.Error(DiagnosticCodes.InvalidEventSourceDeclaration, message, line.Location);

    [GeneratedRegex(@"^eventsource\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex SourceHeaderRegex();

    [GeneratedRegex(@"^stream\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex StreamHeaderRegex();

    [GeneratedRegex(@"^(identifier|streamId)\s+([\w.]+(?:\[\])?(?:\?|\s+optional)?)$", RegexOptions.None, 1000)]
    private static partial Regex TypeDirectiveRegex();

    [GeneratedRegex(@"^id\s+""(" + StringLiteral.BodyPattern + @")""$", RegexOptions.None, 1000)]
    private static partial Regex PinRegex();

    [GeneratedRegex(@"^streamId\s*=\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex MappingRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\s+([\w.]+(?:\[\])?(?:\?|\s+optional)?)$", RegexOptions.None, 1000)]
    private static partial Regex PartDeclarationRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex PartMappingRegex();
}

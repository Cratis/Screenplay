// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>slice</c> declarations and dispatches to the parsers for each construct in the slice body.
/// </summary>
internal static partial class SliceParser
{
    /// <summary>
    /// Parses a slice from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>slice</c> header.</param>
    /// <returns>The parsed <see cref="SliceSyntax"/>.</returns>
    public static SliceSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        var type = SliceType.StateChange;
        var name = string.Empty;

        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSliceDeclaration, $"Invalid slice declaration '{header.Content}' - expected 'slice <Type> <Name>'", header.Location);
        }
        else
        {
            name = match.Groups[2].Value;
            if (!Enum.TryParse(match.Groups[1].Value, out type))
            {
                context.Error(DiagnosticCodes.UnknownSliceType, $"Unknown slice type '{match.Groups[1].Value}' - expected StateChange, StateView, Automation or Translate", header.Location);
                type = SliceType.StateChange;
            }
        }

        TranslationDirection? direction = null;
        var hasDirection = false;
        string? description = null;
        string? documentation = null;
        SourceLocation? descriptionLocation = null;
        int? descriptionRawLength = null;
        var directiveLocations = new Dictionary<string, SourceLocation>();
        var events = new List<EventSyntax>();
        var operations = new List<OperationSyntax>();
        var commands = new List<CommandSyntax>();
        var queries = new List<QuerySyntax>();
        var projections = new List<ProjectionSyntax>();
        var captures = new List<CaptureSyntax>();
        var reactions = new List<ReactionSyntax>();
        var screens = new List<ScreenSyntax>();
        var constraints = new List<ConstraintSyntax>();
        var specifications = new List<SpecificationSyntax>();
        var examples = new List<SpecificationExampleSyntax>();
        var readModels = new List<ReadModelSyntax>();
        var reducers = new List<ReducerSyntax>();
        var templates = new List<TemplateAssignmentSyntax>();
        var purposes = new List<PurposeReferenceSyntax>();
        FileReferenceSyntax? file = null;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (FileReferenceParser.IsDirective(line))
            {
                file = FileReferenceParser.ParseReplacing(context, line, file, directiveLocations);
                continue;
            }

            switch (FirstSliceWord(line.Content))
            {
                case "description":
                    var previousDescription = description;
                    description = DescriptionParser.Parse(context, line, description, $"Slice '{name}'", out var descriptionSpan);
                    if (previousDescription is null && description is not null)
                    {
                        directiveLocations["description"] = line.Location;
                    }

                    if (descriptionSpan is { } span)
                    {
                        descriptionLocation = span.Location;
                        descriptionRawLength = span.RawLength;
                    }

                    break;
                case "documentation":
                    documentation = DocumentationParser.Parse(context, line, documentation, $"Slice '{name}'", directiveLocations);
                    break;
                case "purpose":
                    PurposeParser.ParseReference(context, line, purposes);
                    break;
                case "operation":
                    operations.Add(OperationParser.Parse(context, line).Operation);
                    break;
                case "direction":
                    var directionMatch = DirectionRegex().Match(line.Content);
                    if (hasDirection || type != SliceType.Translate || !directionMatch.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidSliceDeclaration, "Declare direction inbound or direction outbound once, inside a Translate slice only", line.Location);
                    }
                    else
                    {
                        direction = directionMatch.Groups[1].Value == "inbound" ? TranslationDirection.Inbound : TranslationDirection.Outbound;
                        directiveLocations["direction"] = line.Location;
                    }

                    hasDirection = true;
                    break;
                case "public":
                case "event":
                    events.Add(EventParser.Parse(context, line));
                    break;
                case "command":
                    commands.Add(CommandParser.Parse(context, line));
                    break;
                case "query":
                    queries.Add(QueryParser.Parse(context, line));
                    break;
                case "projection":
                    projections.Add(ProjectionParser.Parse(context, line));
                    break;
                case "capture":
                    captures.Add(CaptureParser.Parse(context, line));
                    break;
                case "reaction":
                    reactions.Add(ReactionParser.Parse(context, line));
                    break;
                case "screen":
                    screens.Add(ScreenParser.Parse(context, line));
                    break;
                case "template":
                    if (TemplateAssignmentParser.Parse(context, line) is { } template)
                    {
                        templates.Add(template);
                    }

                    break;
                case "constraint":
                    constraints.Add(ConstraintParser.Parse(context, line));
                    break;
                case "example":
                    examples.Add(SpecificationParser.ParseExample(context, line));
                    break;
                case "specification":
                    specifications.Add(SpecificationParser.Parse(context, line));
                    break;
                case "readmodel":
                    readModels.Add(ReadModelParser.Parse(context, line));
                    break;
                case "reducer":
                    reducers.Add(ReducerParser.Parse(context, line));
                    break;
                default:
                    context.Warning(DiagnosticCodes.UnknownSliceDirective, $"Unknown construct '{LineText.FirstWord(line.Content)}' in slice '{name}'", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(type, name, events, commands, queries, projections, captures, reactions, screens, constraints, specifications, header.Location, description, readModels, reducers)
        {
            Direction = direction,
            Documentation = documentation,
            Examples = examples,
            Operations = operations,
            File = file,
            DescriptionLocation = descriptionLocation,
            DescriptionRawLength = descriptionRawLength,
            Templates = templates,
            Purposes = purposes,
            DirectiveLocations = directiveLocations
        };
    }

    static string FirstSliceWord(string content)
    {
        var end = content.AsSpan().IndexOfAny(' ', '\t');
        var word = end < 0 ? content : content[..end];

        // Only the new metadata forms use tab-separated dispatch. Changing the shared helper would
        // reinterpret legacy property-shaped directives elsewhere (notably command responses).
        return word == "public" || word == "direction" ? word : LineText.FirstWord(content);
    }

    [GeneratedRegex(@"^direction\s+(inbound|outbound)$", RegexOptions.None, 1000)]
    private static partial Regex DirectionRegex();

    [GeneratedRegex(@"^slice\s+([A-Za-z]\w*)\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();
}

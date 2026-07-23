// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
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
            context.Error($"Invalid slice declaration '{header.Content}' - expected 'slice <Type> <Name>'", header.Location);
        }
        else
        {
            name = match.Groups[2].Value;
            if (!Enum.TryParse(match.Groups[1].Value, out type))
            {
                context.Error($"Unknown slice type '{match.Groups[1].Value}' - expected StateChange, StateView, Automation or Translate", header.Location);
                type = SliceType.StateChange;
            }
        }

        string? description = null;
        var events = new List<EventSyntax>();
        var commands = new List<CommandSyntax>();
        var queries = new List<QuerySyntax>();
        ProjectionSyntax? projection = null;
        var captures = new List<CaptureSyntax>();
        var reactors = new List<ReactorSyntax>();
        var screens = new List<ScreenSyntax>();
        var constraints = new List<ConstraintSyntax>();
        var specifications = new List<SpecificationSyntax>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "description":
                    description = DescriptionParser.Parse(context, line, description, $"Slice '{name}'");
                    break;
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
                    if (projection is not null)
                    {
                        context.Error($"Slice '{name}' already declares a projection - a slice can have at most one", line.Location);
                        context.SkipBlock(line.Indent);
                        break;
                    }

                    projection = ProjectionParser.Parse(context, line);
                    break;
                case "capture":
                    captures.Add(CaptureParser.Parse(context, line));
                    break;
                case "reactor":
                    reactors.Add(ReactorParser.Parse(context, line));
                    break;
                case "screen":
                    screens.Add(ScreenParser.Parse(context, line));
                    break;
                case "constraint":
                    constraints.Add(ConstraintParser.Parse(context, line));
                    break;
                case "specification":
                    specifications.Add(SpecificationParser.Parse(context, line));
                    break;
                default:
                    context.Warning($"Unknown construct '{LineText.FirstWord(line.Content)}' in slice '{name}'", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(type, name, events, commands, queries, projection, captures, reactors, screens, constraints, specifications, header.Location, description);
    }

    [GeneratedRegex(@"^slice\s+([A-Za-z]\w*)\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();
}

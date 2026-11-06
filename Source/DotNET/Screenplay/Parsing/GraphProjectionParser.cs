// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>projection &lt;Name&gt; =&gt; graph &lt;Graph&gt;</c> - events projected onto a property graph.
/// </summary>
internal static partial class GraphProjectionParser
{
    static readonly HashSet<string> _readModelOnly = new(StringComparer.Ordinal)
    {
        "children", "nested", "join", "all", "clear", "variant", "key", "sequence", "automap", "no", "parent"
    };

    /// <summary>
    /// Gets whether a projection header targets a graph.
    /// </summary>
    /// <param name="content">The header line content.</param>
    /// <returns>True when the header is <c>projection &lt;Name&gt; =&gt; graph ...</c>.</returns>
    public static bool IsGraphHeader(string content) => GraphTargetRegex().IsMatch(content);

    /// <summary>
    /// Parses a graph projection from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>projection</c> header.</param>
    /// <returns>The parsed <see cref="GraphProjectionSyntax"/>.</returns>
    public static GraphProjectionSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphProjection, $"Invalid graph projection declaration '{header.Content}' - expected 'projection <Name> => graph <Graph>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), string.Empty, [], header.Location) { SourceOptions = context.SourceOptions };
        }

        var name = LineText.Unescape(match.Groups[1].Value);
        var graph = LineText.Unescape(match.Groups[2].Value);
        string? description = null;
        FileReferenceSyntax? file = null;
        var directiveLocations = new Dictionary<string, SourceLocation>();
        var blocks = new List<ProjectionBlockSyntax>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (DescriptionParser.TryParse(context, line, ref description, $"Projection '{name}'", directiveLocations)) continue;
            if (FileReferenceParser.IsDirective(line))
            {
                file = FileReferenceParser.ParseReplacing(context, line, file, directiveLocations);
                continue;
            }

            switch (LineText.FirstWord(line.Content))
            {
                case "node":
                    blocks.Add(ParseElement(context, line, isNode: true));
                    break;
                case "edge":
                    blocks.Add(ParseElement(context, line, isNode: false));
                    break;
                default:
                    Reject(context, line, $"'{LineText.FirstWord(line.Content)}' in a graph projection - a graph projection holds 'node' and 'edge' blocks");
                    break;
            }
        }

        if (blocks.Count == 0)
        {
            context.Error(DiagnosticCodes.InvalidGraphProjection, $"Graph projection '{name}' must contain at least one 'node' or 'edge' block", header.Location);
        }

        return new(name, graph, blocks, header.Location)
        {
            SourceOptions = context.SourceOptions,
            File = file,
            Description = description,
            DirectiveLocations = directiveLocations
        };
    }

    static ProjectionBlockSyntax ParseElement(ParserContext context, SourceLine line, bool isNode)
    {
        var match = (isNode ? NodeRegex() : EdgeRegex()).Match(line.Content);
        var label = match.Success ? LineText.Unescape(match.Groups[1].Value) : string.Empty;
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphProjection, $"Invalid {(isNode ? "node" : "edge")} block '{line.Content}' - expected '{(isNode ? "node <Label>" : "edge <TYPE>")}'", line.Location);
        }

        var handlers = new List<ProjectionBlockSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(child.Content))
            {
                case "from":
                    handlers.Add(ParseFrom(context, child, isNode));
                    break;
                case "every":
                    handlers.Add(ParseEvery(context, child));
                    break;
                case "remove":
                    handlers.Add(ParseRemove(context, child, isNode));
                    break;
                default:
                    Reject(context, child, $"'{LineText.FirstWord(child.Content)}' in a graph {(isNode ? "node" : "edge")} block - expected 'from', 'every' or 'remove with'");
                    break;
            }
        }

        if (match.Success && !handlers.OfType<GraphFromSyntax>().Any())
        {
            context.Error(DiagnosticCodes.InvalidGraphProjection, $"The {(isNode ? "node" : "edge")} block '{label}' must contain at least one 'from' handler", line.Location);
        }

        return isNode ? new GraphNodeBlockSyntax(label, handlers, line.Location) : new GraphEdgeBlockSyntax(label, handlers, line.Location);
    }

    static GraphFromSyntax ParseFrom(ParserContext context, SourceLine line, bool isNode)
    {
        var edge = FromEdgeRegex().Match(line.Content);
        var node = edge.Success ? Match.Empty : FromNodeRegex().Match(line.Content);
        if (!edge.Success && !node.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphProjection, $"Invalid 'from' handler '{line.Content}' - expected {(isNode ? "'from <Event> key <expression>'" : "'from <Event> source <expression> target <expression>'")}", line.Location);
            context.SkipBlock(line.Indent);
            return new(string.Empty, null, null, null, [], line.Location);
        }

        var mappings = ParseMappings(context, line);
        ExpressionSyntax Expression(Group group) => ExpressionParser.ParseProjectionExpression(context, group.Value, line.Location);
        if (edge.Success)
        {
            if (isNode) WrongForm(context, line, "a node is identified by 'key', not by source and target");
            return new(LineText.Unescape(edge.Groups[1].Value), null, Expression(edge.Groups[3]), Expression(edge.Groups[4]), mappings, line.Location)
            {
                Identity = edge.Groups[5].Success ? Expression(edge.Groups[5]) : null,
                ForEach = ForEach(context, edge.Groups[2], line)
            };
        }

        if (!isNode) WrongForm(context, line, "an edge is identified by 'source' and 'target', not by 'key'");
        return new(LineText.Unescape(node.Groups[1].Value), Expression(node.Groups[3]), null, null, mappings, line.Location)
        {
            ForEach = ForEach(context, node.Groups[2], line)
        };
    }

    static EverySyntax ParseEvery(ParserContext context, SourceLine line) =>
        new(ParseMappings(context, line), false, AutoMapMode.Inherit, line.Location);

    static GraphRemoveSyntax ParseRemove(ParserContext context, SourceLine line, bool isNode)
    {
        var edge = RemoveEdgeRegex().Match(line.Content);
        var node = edge.Success ? Match.Empty : RemoveNodeRegex().Match(line.Content);
        if (!edge.Success && !node.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphProjection, $"Invalid removal '{line.Content}' - expected {(isNode ? "'remove with <Event> key <expression> [detach]'" : "'remove with <Event> source <expression> target <expression>'")}", line.Location);
            context.SkipBlock(line.Indent);
            return new(string.Empty, null, null, null, false, line.Location);
        }

        if (context.TryPeekChild(line.Indent, out var child))
        {
            context.Error(DiagnosticCodes.InvalidGraphProjection, "A graph removal has no body", child.Location);
            context.SkipBlock(line.Indent);
        }

        ExpressionSyntax Expression(Group group) => ExpressionParser.ParseProjectionExpression(context, group.Value, line.Location);
        if (edge.Success)
        {
            if (isNode) WrongForm(context, line, "a node is identified by 'key', not by source and target");
            if (edge.Groups[6].Success && !isNode)
            {
                context.Error(DiagnosticCodes.InvalidGraphEdgeRemoval, "'detach' removes the edges attached to a node; it cannot be written on an edge removal", line.Location);
            }

            return new(LineText.Unescape(edge.Groups[1].Value), null, Expression(edge.Groups[3]), Expression(edge.Groups[4]), edge.Groups[6].Success, line.Location)
            {
                Identity = edge.Groups[5].Success ? Expression(edge.Groups[5]) : null,
                ForEach = ForEach(context, edge.Groups[2], line)
            };
        }

        if (!isNode) WrongForm(context, line, "an edge is identified by 'source' and 'target', not by 'key'");
        if (node.Groups[4].Success && !isNode)
        {
            context.Error(DiagnosticCodes.InvalidGraphEdgeRemoval, "'detach' removes the edges attached to a node; it cannot be written on an edge removal", line.Location);
        }

        return new(LineText.Unescape(node.Groups[1].Value), Expression(node.Groups[3]), null, null, node.Groups[4].Success, line.Location)
        {
            ForEach = ForEach(context, node.Groups[2], line)
        };
    }

    // One level only: the collection is a property of the event, never a property of an item (nested iteration is refused, PLAY0886).
    static string? ForEach(ParserContext context, Group group, SourceLine line)
    {
        if (!group.Success) return null;
        var collection = LineText.Unescape(group.Value);
        if (collection.Contains('.', StringComparison.Ordinal))
        {
            context.Error(DiagnosticCodes.UnsupportedGraphForEach, $"'foreach {collection}' iterates a collection inside another; 'foreach' names a property of the event itself", line.Location);
        }

        return collection;
    }

    static List<MappingSyntax> ParseMappings(ParserContext context, SourceLine parent)
    {
        var mappings = new List<MappingSyntax>();
        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (string.Equals(child.Content, "automap", StringComparison.Ordinal) || string.Equals(child.Content, "no automap", StringComparison.Ordinal))
            {
                Reject(context, child, $"'{child.Content}' in a graph projection - properties of a node or edge are matched by name");
            }
            else if (ProjectionParser.ParseGraphMapping(context, child) is { } mapping)
            {
                mappings.Add(mapping);
            }
        }

        return mappings;
    }

    static void WrongForm(ParserContext context, SourceLine line, string reason) =>
        context.Error(DiagnosticCodes.InvalidGraphProjection, $"Invalid handler '{line.Content}' - {reason}", line.Location);

    static void Reject(ParserContext context, SourceLine line, string what)
    {
        var word = LineText.FirstWord(line.Content);
        context.Error(_readModelOnly.Contains(word) ? DiagnosticCodes.UnsupportedGraphProjectionConstruct : DiagnosticCodes.InvalidGraphProjection, $"Unexpected {what}", line.Location);
        context.SkipBlock(line.Indent);
    }

    [GeneratedRegex(@"^projection\s+(@?[\w.]+)\s*=>\s*graph\b", RegexOptions.NonBacktracking)]
    private static partial Regex GraphTargetRegex();

    [GeneratedRegex(@"^projection\s+(@?[\w.]+)\s*=>\s*graph\s+(@?[\w.]+)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^node\s+(@?[A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex NodeRegex();

    [GeneratedRegex(@"^edge\s+(@?[A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex EdgeRegex();

    [GeneratedRegex(@"^from\s+(@?[\w.]+)(?:\s+foreach\s+(@?[\w.]+))?\s+key\s+(.+)$", RegexOptions.NonBacktracking)]
    private static partial Regex FromNodeRegex();

    [GeneratedRegex(@"^from\s+(@?[\w.]+)(?:\s+foreach\s+(@?[\w.]+))?\s+source\s+(.+?)\s+target\s+(.+?)(?:\s+identified\s+by\s+(.+))?$", RegexOptions.NonBacktracking)]
    private static partial Regex FromEdgeRegex();

    [GeneratedRegex(@"^remove\s+with\s+(@?[\w.]+)(?:\s+foreach\s+(@?[\w.]+))?\s+key\s+(.+?)(\s+detach)?$", RegexOptions.NonBacktracking)]
    private static partial Regex RemoveNodeRegex();

    [GeneratedRegex(@"^remove\s+with\s+(@?[\w.]+)(?:\s+foreach\s+(@?[\w.]+))?\s+source\s+(.+?)\s+target\s+(.+?)(?:\s+identified\s+by\s+(.+?))?(\s+detach)?$", RegexOptions.NonBacktracking)]
    private static partial Regex RemoveEdgeRegex();
}

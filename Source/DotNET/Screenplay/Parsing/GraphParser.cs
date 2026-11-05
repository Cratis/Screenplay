// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>graph</c> declarations - the shape of a property graph, with its node labels and edge types.
/// </summary>
internal static partial class GraphParser
{
    /// <summary>
    /// Parses a graph from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>graph</c> header.</param>
    /// <returns>The parsed <see cref="GraphSyntax"/>.</returns>
    public static GraphSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        var name = match.Success ? LineText.Unescape(match.Groups[1].Value) : LineText.FirstWord(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphDeclaration, $"Invalid graph declaration '{header.Content}' - expected 'graph <Name>'", header.Location);
        }

        var nodes = new List<GraphNodeSyntax>();
        var edges = new List<GraphEdgeSyntax>();
        string? description = null;
        string? documentation = null;
        FileReferenceSyntax? file = null;
        var directiveLocations = new Dictionary<string, SourceLocation>();

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "description":
                    var previous = description;
                    description = DescriptionParser.Parse(context, line, description, $"Graph '{name}'");
                    if (previous is null && description is not null) directiveLocations["description"] = line.Location;
                    break;
                case "documentation":
                    documentation = DocumentationParser.Parse(context, line, documentation, $"Graph '{name}'", directiveLocations);
                    break;
                case "node":
                    nodes.Add(ParseNode(context, line));
                    break;
                case "edge":
                    edges.Add(ParseEdge(context, line));
                    break;
                default:
                    if (FileReferenceParser.IsDirective(line))
                    {
                        file = FileReferenceParser.ParseReplacing(context, line, file, directiveLocations);
                        break;
                    }

                    context.Error(DiagnosticCodes.InvalidGraphDeclaration, $"Unexpected '{line.Content}' in graph '{name}' - expected 'node <Label>' or 'edge <TYPE> source <Label> target <Label>'", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        if (nodes.Count == 0 && match.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphDeclaration, $"Graph '{name}' must declare at least one node", header.Location);
        }

        return new(name, nodes, edges, header.Location, description) { Documentation = documentation, File = file, DirectiveLocations = directiveLocations };
    }

    static GraphNodeSyntax ParseNode(ParserContext context, SourceLine line)
    {
        var match = NodeRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphDeclaration, $"Invalid node declaration '{line.Content}' - expected 'node <Label>'", line.Location);
        }

        var label = match.Success ? LineText.Unescape(match.Groups[1].Value) : string.Empty;
        return new(label, ParseProperties(context, line, allowKey: true), line.Location);
    }

    static GraphEdgeSyntax ParseEdge(ParserContext context, SourceLine line)
    {
        var match = EdgeRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphDeclaration, $"Invalid edge declaration '{line.Content}' - expected 'edge <TYPE> source <Label> target <Label> [identified by <property>]'", line.Location);
            context.SkipBlock(line.Indent);
            return new(string.Empty, string.Empty, string.Empty, [], line.Location);
        }

        return new(
            LineText.Unescape(match.Groups[1].Value),
            LineText.Unescape(match.Groups[2].Value),
            LineText.Unescape(match.Groups[3].Value),
            ParseProperties(context, line, allowKey: false),
            line.Location)
        {
            IdentifiedBy = match.Groups[4].Success ? LineText.Unescape(match.Groups[4].Value) : null
        };
    }

    static List<PropertySyntax> ParseProperties(ParserContext context, SourceLine owner, bool allowKey)
    {
        var properties = new List<PropertySyntax>();
        while (context.TryPeekChild(owner.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (PropertyLineParser.Parse(context, line) is not { } property)
            {
                context.Error(DiagnosticCodes.InvalidGraphDeclaration, $"Invalid property '{line.Content}' - expected '<name> <Type>'", line.Location);
                continue;
            }

            if ((property.IsKey && !allowKey) || property.IsGenerated || property.IsIdentifier || property.IsSubject)
            {
                var modifier = property.IsKey && !allowKey ? "key (an edge is identified by its endpoints, or by 'identified by')" : "generated, identifier and subject";
                context.Error(DiagnosticCodes.InvalidGraphDeclaration, $"Property '{property.Name}' cannot use the modifier {modifier} in a graph", line.Location);
            }

            properties.Add(property);
        }

        return properties;
    }

    [GeneratedRegex(@"^graph\s+(@?[A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^node\s+(@?[A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex NodeRegex();

    [GeneratedRegex(@"^edge\s+(@?[A-Za-z_]\w*)\s+source\s+(@?[A-Za-z_]\w*)\s+target\s+(@?[A-Za-z_]\w*)(?:\s+identified\s+by\s+(@?[A-Za-z_]\w*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex EdgeRegex();
}

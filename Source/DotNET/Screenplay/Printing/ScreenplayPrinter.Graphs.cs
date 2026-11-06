// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of graph declarations, graph projections and graph specification steps.
/// </summary>
public partial class ScreenplayPrinter
{
    static string GraphIdentity(ScreenplayWriter writer, string? forEach, ExpressionSyntax? key, ExpressionSyntax? source, ExpressionSyntax? target, ExpressionSyntax? identity)
    {
        var text = forEach is null ? string.Empty : $" foreach {forEach}";
        if (key is not null) text += $" key {writer.Expression(key)}";
        if (source is not null) text += $" source {writer.Expression(source)}";
        if (target is not null) text += $" target {writer.Expression(target)}";
        if (identity is not null) text += $" identified by {writer.Expression(identity)}";
        return text;
    }

    static string PatternNode(GraphPatternNodeSyntax node) => node.Alias is null ? node.Label : $"{node.Alias}:{node.Label}";

    static string Pattern(GraphMatchSyntax match) =>
        PatternNode(match.Start) + string.Concat(match.Hops.Select(hop =>
        {
            var edge = hop.Alias is null ? hop.Type : $"{hop.Alias}:{hop.Type}";
            return $" {(hop.Outgoing ? $"-{edge}->" : $"<-{edge}-")} {PatternNode(hop.Node)}";
        }));

    void WriteGraphQuery(ScreenplayWriter writer, GraphQuerySyntax query)
    {
        using var anchor = writer.Anchor(query);
        var returnType = ScreenplaySyntaxText.TypeRef(query.ReturnType);
        writer.Line($"query {query.Name} => {(query.IsObservable ? "observable " : string.Empty)}{returnType} graph {query.Graph}");
        using (writer.Indent())
        {
            WriteDescription(writer, query.Description, query);
            if (query.By is not null) writer.Line($"by {ScreenplaySyntaxText.QueryParameter(query.By)}", query.By);
            foreach (var filter in query.Filters) writer.Line($"filter {ScreenplaySyntaxText.QueryParameter(filter)}", filter);
            if (query.Authorize is not null) WriteAuthorize(writer, query.Authorize);
            foreach (var match in query.Matches) writer.Line($"{(match.IsOptional ? "optional match" : "match")} {Pattern(match)}", match);
            if (query.Conditions.Any())
            {
                writer.Line("where " + string.Join(" and ", query.Conditions.Select(condition =>
                    $"{condition.Alias}.{condition.Property} = {(condition.Value is PathExpressionSyntax argument ? "$" + argument.Path : writer.Expression(condition.Value))}")));
            }

            writer.Line("return");
            using (writer.Indent())
            {
                foreach (var @return in query.Returns) writer.Line($"{@return.Property} = {@return.Alias}.{@return.Source}", @return);
            }
        }
    }

    void WriteGraph(ScreenplayWriter writer, GraphSyntax graph)
    {
        using var anchor = writer.Anchor(graph);
        writer.Line($"graph {graph.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, graph.Description, graph);
            WriteDocumentation(writer, graph.Documentation, graph);
            WriteFile(writer, graph.File);
            foreach (var node in graph.Nodes)
            {
                using var nodeAnchor = writer.Anchor(node);
                writer.Line($"node {node.Label}");
                using (writer.Indent()) WriteProperties(writer, node.Properties, ReservedWords.None);
            }

            foreach (var edge in graph.Edges)
            {
                using var edgeAnchor = writer.Anchor(edge);
                var identity = edge.IdentifiedBy is null ? string.Empty : $" identified by {edge.IdentifiedBy}";
                writer.Line($"edge {edge.Type} source {edge.Source} target {edge.Target}{identity}");
                using (writer.Indent()) WriteProperties(writer, edge.Properties, ReservedWords.None);
            }
        }
    }

    void WriteGraphProjection(ScreenplayWriter writer, GraphProjectionSyntax projection)
    {
        using var anchor = writer.Anchor(projection);
        writer.Line($"projection {projection.Name} => graph {projection.Graph}");
        using (writer.Indent())
        {
            WriteDescription(writer, projection.Description, projection);
            WriteFile(writer, projection.File);
            foreach (var block in projection.Blocks) WriteGraphBlock(writer, block);
        }
    }

    void WriteGraphBlock(ScreenplayWriter writer, ProjectionBlockSyntax block)
    {
        using var anchor = writer.Anchor(block);
        switch (block)
        {
            case GraphNodeBlockSyntax node:
                writer.Line($"node {node.Label}");
                using (writer.Indent()) foreach (var handler in node.Blocks) WriteGraphBlock(writer, handler);
                break;
            case GraphEdgeBlockSyntax edge:
                writer.Line($"edge {edge.Type}");
                using (writer.Indent()) foreach (var handler in edge.Blocks) WriteGraphBlock(writer, handler);
                break;
            case GraphFromSyntax from:
                writer.Line($"from {from.Event}{GraphIdentity(writer, from.ForEach, from.Key, from.Source, from.Target, from.Identity)}");
                using (writer.Indent()) WriteMappings(writer, from.Mappings, ReservedWords.None);
                break;
            case EverySyntax every:
                writer.Line("every");
                using (writer.Indent()) WriteMappings(writer, every.Mappings, ReservedWords.None);
                break;
            case GraphRemoveSyntax remove:
                writer.Line($"remove with {remove.Event}{GraphIdentity(writer, remove.ForEach, remove.Key, remove.Source, remove.Target, remove.Identity)}{(remove.Detach ? " detach" : string.Empty)}");
                break;
            default:
                throw new UnsupportedSyntaxForPrinting("graph projection block", block.GetType().Name);
        }
    }

    void WriteSpecificationGraph(ScreenplayWriter writer, string keyword, SpecificationGraphSyntax graph)
    {
        using var anchor = writer.Anchor(graph);
        writer.Line($"{keyword} graph {graph.Graph}");
        using (writer.Indent())
        {
            foreach (var element in graph.Elements)
            {
                using var elementAnchor = writer.Anchor(element);
                var prefix = element.Absent ? "no" : "has";
                writer.Line(element switch
                {
                    SpecificationGraphNodeSyntax node => $"{prefix} node {node.Label} {writer.Expression(node.Key)}",
                    SpecificationGraphEdgeSyntax edge => $"{prefix} edge {edge.Type} from {writer.Expression(edge.Source)} to {writer.Expression(edge.Target)}{(edge.Identity is null ? string.Empty : $" identified by {writer.Expression(edge.Identity)}")}",
                    _ => throw new UnsupportedSyntaxForPrinting("graph specification step", element.GetType().Name)
                });
                using (writer.Indent()) WriteSpecificationValues(writer, element.Properties);
            }
        }
    }
}

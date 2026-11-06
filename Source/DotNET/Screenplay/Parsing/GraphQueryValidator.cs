// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks a graph query against the graph it reads and the read model it returns: labels, edge types and directions,
/// aliases, arguments and the properties of every row.
/// </summary>
internal static class GraphQueryValidator
{
    internal static void Validate(GraphQuerySyntax query, Dictionary<string, GraphSyntax> graphs, Dictionary<string, ReadModelSyntax> readModels, ParserContext context)
    {
        if (query.Graph.Length == 0) return;
        if (!graphs.TryGetValue(query.Graph, out var graph))
        {
            context.Error(DiagnosticCodes.UnknownGraphQueryReference, $"Query '{query.Name}' reads unknown graph '{query.Graph}' - declare it with 'graph {query.Graph}'", query.Location);
            return;
        }

        var aliases = new Dictionary<string, (bool IsNode, string Name, bool Optional)>(StringComparer.Ordinal);
        foreach (var match in query.Matches)
        {
            var previous = match.Start.Label;
            CheckNode(match.Start, match.IsOptional);
            foreach (var hop in match.Hops)
            {
                var edge = graph.Edges.FirstOrDefault(candidate => candidate.Type == hop.Type);
                if (edge is null)
                {
                    context.Error(DiagnosticCodes.UnknownGraphQueryReference, $"Graph '{graph.Name}' does not declare edge '{hop.Type}'", hop.Location);
                }
                else
                {
                    var (from, to) = hop.Outgoing ? (previous, hop.Node.Label) : (hop.Node.Label, previous);
                    if (edge.Source != from || edge.Target != to)
                    {
                        context.Error(DiagnosticCodes.GraphQueryEdgeMismatch, $"Edge '{hop.Type}' of graph '{graph.Name}' goes from '{edge.Source}' to '{edge.Target}', not from '{from}' to '{to}'", hop.Location);
                    }
                }

                if (hop.Alias is not null) Declare(hop.Alias, (false, hop.Type, match.IsOptional), hop.Location);
                CheckNode(hop.Node, match.IsOptional);
                previous = hop.Node.Label;
            }
        }

        var arguments = query.Filters.Concat(query.By is null ? [] : [query.By]).ToDictionary(argument => argument.Name, StringComparer.Ordinal);
        foreach (var condition in query.Conditions)
        {
            var property = Property(graph, condition.Alias, condition.Property, condition.Location);
            if (condition.Value is PathExpressionSyntax argument)
            {
                if (!arguments.TryGetValue(argument.Path, out var declared))
                {
                    context.Error(DiagnosticCodes.GraphQueryArgumentMismatch, $"Condition uses '${argument.Path}', which query '{query.Name}' does not declare - declare it with 'by' or 'filter'", condition.Location);
                }
                else if (property is not null && declared.Type.Name != property.Type.Name)
                {
                    context.Error(DiagnosticCodes.GraphQueryArgumentMismatch, $"'${argument.Path}' is a {declared.Type.Name}, but '{condition.Alias}.{condition.Property}' is a {property.Type.Name}", condition.Location);
                }
            }
        }

        ValidateReturns(query, graph, readModels, Property, aliases, context);

        void Declare(string alias, (bool IsNode, string Name, bool Optional) target, SourceLocation location)
        {
            if (!aliases.TryAdd(alias, target))
            {
                context.Error(DiagnosticCodes.DuplicateGraphQueryAlias, $"Alias '{alias}' is used twice in query '{query.Name}'", location);
            }
        }

        void CheckNode(GraphPatternNodeSyntax node, bool optional)
        {
            if (graph.Nodes.All(candidate => candidate.Label != node.Label))
            {
                context.Error(DiagnosticCodes.UnknownGraphQueryReference, $"Graph '{graph.Name}' does not declare node '{node.Label}'", node.Location);
            }

            if (node.Alias is null) return;

            // A node that an earlier pattern already binds is the same node again: that is how two paths share a start.
            if (aliases.TryGetValue(node.Alias, out var existing) && existing.IsNode && existing.Name == node.Label) return;
            Declare(node.Alias, (true, node.Label, optional), node.Location);
        }

        PropertySyntax? Property(GraphSyntax declaring, string alias, string name, SourceLocation location) => ResolveProperty(declaring, aliases, alias, name, location, context);
    }

    static PropertySyntax? ResolveProperty(GraphSyntax graph, Dictionary<string, (bool IsNode, string Name, bool Optional)> aliases, string alias, string name, SourceLocation location, ParserContext context)
    {
        if (!aliases.TryGetValue(alias, out var target))
        {
            context.Error(DiagnosticCodes.UnknownGraphQueryReference, $"'{alias}' is not an alias of the pattern - name it as '{alias}:Label' in a 'match'", location);
            return null;
        }

        var properties = target.IsNode
            ? graph.Nodes.FirstOrDefault(candidate => candidate.Label == target.Name)?.Properties
            : graph.Edges.FirstOrDefault(candidate => candidate.Type == target.Name)?.Properties;
        var property = properties?.FirstOrDefault(candidate => candidate.Name == name);
        if (property is null && properties is not null)
        {
            context.Error(DiagnosticCodes.UnknownGraphQueryReference, $"{(target.IsNode ? "Node" : "Edge")} '{target.Name}' does not declare property '{name}'", location);
        }

        return property;
    }

    static void ValidateReturns(GraphQuerySyntax query, GraphSyntax graph, Dictionary<string, ReadModelSyntax> readModels, Func<GraphSyntax, string, string, SourceLocation, PropertySyntax?> property, Dictionary<string, (bool IsNode, string Name, bool Optional)> aliases, ParserContext context)
    {
        if (!readModels.TryGetValue(query.ReturnType.Name, out var readModel)) return;
        var declared = readModel.Properties.ToDictionary(candidate => candidate.Name, StringComparer.Ordinal);
        var assigned = new HashSet<string>(StringComparer.Ordinal);
        foreach (var @return in query.Returns)
        {
            if (!assigned.Add(@return.Property))
            {
                context.Error(DiagnosticCodes.GraphQueryReturnMismatch, $"'{@return.Property}' is returned twice", @return.Location);
                continue;
            }

            if (!declared.TryGetValue(@return.Property, out var target))
            {
                context.Error(DiagnosticCodes.GraphQueryReturnMismatch, $"Read model '{readModel.Name}' has no property '{@return.Property}'", @return.Location);
                continue;
            }

            var source = property(graph, @return.Alias, @return.Source, @return.Location);
            if (source is null) continue;
            if (aliases.TryGetValue(@return.Alias, out var origin) && origin.Optional && !target.Type.IsOptional)
            {
                context.Error(DiagnosticCodes.GraphQueryOptionalReturn, $"'{@return.Alias}' comes from an 'optional match', so '{@return.Property}' can be absent - make it optional on read model '{readModel.Name}'", @return.Location);
            }

            if (source.Type.Name != target.Type.Name || source.Type.IsCollection != target.Type.IsCollection)
            {
                context.Error(DiagnosticCodes.GraphQueryReturnMismatch, $"'{@return.Property}' is a {ScreenplaySyntaxText(target)}, but '{@return.Alias}.{@return.Source}' is a {ScreenplaySyntaxText(source)}", @return.Location);
            }
        }

        foreach (var missing in declared.Values.Where(candidate => !candidate.Type.IsOptional && !assigned.Contains(candidate.Name)))
        {
            context.Error(DiagnosticCodes.GraphQueryReturnMismatch, $"Query '{query.Name}' does not return '{missing.Name}', which read model '{readModel.Name}' requires", query.Location);
        }
    }

    static string ScreenplaySyntaxText(PropertySyntax property) => property.Type.Name + (property.Type.IsCollection ? "[]" : string.Empty);
}

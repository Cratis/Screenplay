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
    internal static void Validate(GraphQuerySyntax query, Dictionary<string, GraphSyntax> graphs, Dictionary<string, ReadModelSyntax> readModels, Dictionary<string, TypeSyntax> types, ParserContext context)
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

        var collects = query.Collects.ToList();
        var root = collects.Count > 0 ? ValidateCollectShape(query, context) : null;
        foreach (var collect in collects)
        {
            if (root is not null && collect.From != root.Alias)
            {
                context.Error(DiagnosticCodes.InvalidGraphCollect, $"Collect '{collect.Property}' starts at '{collect.From}', but query '{query.Name}' collects from its root '{root.Alias}'", collect.Location);
            }

            var edge = graph.Edges.FirstOrDefault(candidate => candidate.Type == collect.Hop.Type);
            if (edge is null)
            {
                context.Error(DiagnosticCodes.UnknownGraphQueryReference, $"Graph '{graph.Name}' does not declare edge '{collect.Hop.Type}'", collect.Hop.Location);
            }
            else if (root is not null)
            {
                var (from, to) = collect.Hop.Outgoing ? (root.Label, collect.Hop.Node.Label) : (collect.Hop.Node.Label, root.Label);
                if (edge.Source != from || edge.Target != to)
                {
                    context.Error(DiagnosticCodes.GraphQueryEdgeMismatch, $"Edge '{collect.Hop.Type}' of graph '{graph.Name}' goes from '{edge.Source}' to '{edge.Target}', not from '{from}' to '{to}'", collect.Hop.Location);
                }
            }

            if (collect.Hop.Alias is not null) Declare(collect.Hop.Alias, (false, collect.Hop.Type, false), collect.Hop.Location);
            if (graph.Nodes.All(candidate => candidate.Label != collect.Hop.Node.Label))
            {
                context.Error(DiagnosticCodes.UnknownGraphQueryReference, $"Graph '{graph.Name}' does not declare node '{collect.Hop.Node.Label}'", collect.Hop.Node.Location);
            }

            if (collect.Hop.Node.Alias is not null) Declare(collect.Hop.Node.Alias, (true, collect.Hop.Node.Label, false), collect.Hop.Node.Location);
        }

        if (root is not null) ValidateCollectAliases(query, root, collects, context);

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
        if (collects.Count > 0) ValidateCollects(query, graph, readModels, types, Property, context);

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

        var collected = query.Collects.Select(collect => collect.Property).ToHashSet(StringComparer.Ordinal);
        foreach (var missing in declared.Values.Where(candidate => !candidate.Type.IsOptional && !assigned.Contains(candidate.Name) && !collected.Contains(candidate.Name)))
        {
            context.Error(DiagnosticCodes.GraphQueryReturnMismatch, $"Query '{query.Name}' does not return '{missing.Name}', which read model '{readModel.Name}' requires", query.Location);
        }
    }

    // A query that collects reads like a view: one 'match' of its root node alone, so every row is one root.
    static GraphPatternNodeSyntax? ValidateCollectShape(GraphQuerySyntax query, ParserContext context)
    {
        var matches = query.Matches.ToList();
        if (matches.Count != 1 || matches[0].IsOptional || matches[0].Hops.Any() || matches[0].Start.Alias is null)
        {
            context.Error(DiagnosticCodes.GraphCollectQueryShape, $"Query '{query.Name}' collects, so it needs exactly one 'match <alias>:<Label>' of its root node alone - follow edges from the root with 'collect'", query.Location);
            return matches.Count > 0 && matches[0].Start.Alias is not null ? matches[0].Start : null;
        }

        return matches[0].Start;
    }

    static void ValidateCollectAliases(GraphQuerySyntax query, GraphPatternNodeSyntax root, List<GraphCollectSyntax> collects, ParserContext context)
    {
        var edges = collects.Where(collect => collect.Hop.Alias is not null).Select(collect => collect.Hop.Alias).ToHashSet(StringComparer.Ordinal);
        foreach (var condition in query.Conditions.Where(condition => edges.Contains(condition.Alias)))
        {
            context.Error(DiagnosticCodes.GraphCollectQueryShape, $"Condition on '{condition.Alias}' names a collected edge - a condition selects roots by a property of the root or of a collected node", condition.Location);
        }

        foreach (var @return in query.Returns.Where(@return => @return.Alias != root.Alias))
        {
            context.Error(DiagnosticCodes.GraphCollectQueryShape, $"'{@return.Property}' is returned from '{@return.Alias}', but a query that collects returns its scalar properties from its root '{root.Alias}'", @return.Location);
        }
    }

    static void ValidateCollects(GraphQuerySyntax query, GraphSyntax graph, Dictionary<string, ReadModelSyntax> readModels, Dictionary<string, TypeSyntax> types, Func<GraphSyntax, string, string, SourceLocation, PropertySyntax?> property, ParserContext context)
    {
        if (!readModels.TryGetValue(query.ReturnType.Name, out var readModel)) return;
        var declared = readModel.Properties.ToDictionary(candidate => candidate.Name, StringComparer.Ordinal);
        var assigned = query.Returns.Select(@return => @return.Property).ToHashSet(StringComparer.Ordinal);
        foreach (var collect in query.Collects)
        {
            if (!assigned.Add(collect.Property))
            {
                context.Error(DiagnosticCodes.GraphQueryReturnMismatch, $"'{collect.Property}' is returned twice", collect.Location);
                continue;
            }

            if (!declared.TryGetValue(collect.Property, out var target))
            {
                context.Error(DiagnosticCodes.GraphQueryReturnMismatch, $"Read model '{readModel.Name}' has no property '{collect.Property}'", collect.Location);
                continue;
            }

            if (!types.TryGetValue(target.Type.Name, out var row) || (!target.Type.IsCollection && !target.Type.IsOptional))
            {
                context.Error(DiagnosticCodes.GraphCollectTargetMismatch, $"'{collect.Property}' of read model '{readModel.Name}' must be a list of a composite type ('{target.Type.Name}[]') or an optional composite type ('{target.Type.Name} optional') to be collected", collect.Location);
                continue;
            }

            var rowProperties = row.Properties.ToDictionary(candidate => candidate.Name, StringComparer.Ordinal);
            var rowAssigned = new HashSet<string>(StringComparer.Ordinal);
            foreach (var @return in collect.Returns)
            {
                if (@return.Alias != collect.Hop.Alias && @return.Alias != collect.Hop.Node.Alias)
                {
                    context.Error(DiagnosticCodes.InvalidGraphCollect, $"'{@return.Property}' of collect '{collect.Property}' reads '{@return.Alias}' - a collected row reads the collected node or edge only", @return.Location);
                    continue;
                }

                if (!rowAssigned.Add(@return.Property))
                {
                    context.Error(DiagnosticCodes.GraphCollectTargetMismatch, $"'{@return.Property}' of collect '{collect.Property}' is assigned twice", @return.Location);
                    continue;
                }

                if (!rowProperties.TryGetValue(@return.Property, out var rowTarget))
                {
                    context.Error(DiagnosticCodes.GraphCollectTargetMismatch, $"Type '{row.Name}' has no property '{@return.Property}'", @return.Location);
                    continue;
                }

                var source = property(graph, @return.Alias, @return.Source, @return.Location);
                if (source is not null && (source.Type.Name != rowTarget.Type.Name || source.Type.IsCollection != rowTarget.Type.IsCollection))
                {
                    context.Error(DiagnosticCodes.GraphCollectTargetMismatch, $"'{@return.Property}' of type '{row.Name}' is a {ScreenplaySyntaxText(rowTarget)}, but '{@return.Alias}.{@return.Source}' is a {ScreenplaySyntaxText(source)}", @return.Location);
                }
            }

            foreach (var missing in row.Properties.Where(candidate => !candidate.Type.IsOptional && !rowAssigned.Contains(candidate.Name)))
            {
                context.Error(DiagnosticCodes.GraphCollectTargetMismatch, $"Collect '{collect.Property}' does not assign '{missing.Name}', which type '{row.Name}' requires", collect.Location);
            }
        }
    }

    static string ScreenplaySyntaxText(PropertySyntax property) => property.Type.Name + (property.Type.IsCollection ? "[]" : string.Empty);
}

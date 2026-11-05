// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates a graph projection against the graph it builds.
/// </summary>
internal static class GraphProjectionValidator
{
    internal static void Validate(GraphProjectionSyntax projection, Dictionary<string, GraphSyntax> graphs, DocumentNames names, ParserContext context)
    {
        if (projection.Graph.Length == 0) return;
        if (!graphs.TryGetValue(projection.Graph, out var graph))
        {
            context.Error(DiagnosticCodes.UnknownGraphProjectionTarget, $"Projection '{projection.Name}' builds graph '{projection.Graph}', which is not declared - declare it with 'graph {projection.Graph}'", projection.Location);
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var block in projection.Blocks)
        {
            switch (block)
            {
                case GraphNodeBlockSyntax node:
                    ValidateNode(node, graph, seen, names, context);
                    break;
                case GraphEdgeBlockSyntax edge:
                    ValidateEdge(edge, graph, seen, names, context);
                    break;
            }
        }
    }

    static void ValidateNode(GraphNodeBlockSyntax block, GraphSyntax graph, HashSet<string> seen, DocumentNames names, ParserContext context)
    {
        if (block.Label.Length == 0) return;
        if (!seen.Add("node " + block.Label)) context.Error(DiagnosticCodes.DuplicateGraphElement, $"Node '{block.Label}' is built more than once by projection of graph '{graph.Name}'", block.Location);
        var declared = graph.Nodes.FirstOrDefault(node => node.Label == block.Label);
        if (declared is null)
        {
            context.Error(DiagnosticCodes.UnknownGraphProjectionElement, $"Graph '{graph.Name}' does not declare node '{block.Label}'", block.Location);
            return;
        }

        var attached = graph.Edges.Any(edge => edge.Source == block.Label || edge.Target == block.Label);
        foreach (var handler in block.Blocks)
        {
            ValidateHandler(handler, declared.Properties, names, context);
            if (handler is GraphRemoveSyntax { Detach: false } remove && attached)
            {
                context.Warning(DiagnosticCodes.MissingGraphDetach, $"Removing node '{block.Label}' without 'detach' leaves its edges dangling - write 'remove with {remove.Event} key ... detach'", remove.Location);
            }
        }
    }

    static void ValidateEdge(GraphEdgeBlockSyntax block, GraphSyntax graph, HashSet<string> seen, DocumentNames names, ParserContext context)
    {
        if (block.Type.Length == 0) return;
        if (!seen.Add("edge " + block.Type)) context.Error(DiagnosticCodes.DuplicateGraphElement, $"Edge '{block.Type}' is built more than once by projection of graph '{graph.Name}'", block.Location);
        var declared = graph.Edges.FirstOrDefault(edge => edge.Type == block.Type);
        if (declared is null)
        {
            context.Error(DiagnosticCodes.UnknownGraphProjectionElement, $"Graph '{graph.Name}' does not declare edge '{block.Type}'", block.Location);
            return;
        }

        foreach (var handler in block.Blocks)
        {
            ValidateHandler(handler, declared.Properties, names, context);
            var (identity, location) = handler switch
            {
                GraphFromSyntax from => (from.Identity, from.Location),
                GraphRemoveSyntax remove => (remove.Identity, remove.Location),
                _ => (null, handler.Location)
            };
            if (handler is GraphFromSyntax or GraphRemoveSyntax)
            {
                if (declared.IdentifiedBy is null && identity is not null)
                {
                    context.Error(DiagnosticCodes.UnknownGraphEdgeIdentity, $"Edge '{declared.Type}' declares no 'identified by', so a handler cannot supply an identity", location);
                }
                else if (declared.IdentifiedBy is not null && identity is null)
                {
                    context.Error(DiagnosticCodes.InvalidGraphProjection, $"Edge '{declared.Type}' is identified by '{declared.IdentifiedBy}' - write 'identified by <expression>' on the handler", location);
                }
            }
        }
    }

    static void ValidateHandler(ProjectionBlockSyntax handler, IEnumerable<PropertySyntax> properties, DocumentNames names, ParserContext context)
    {
        var eventName = handler switch { GraphFromSyntax from => from.Event, GraphRemoveSyntax remove => remove.Event, _ => null };
        if (!string.IsNullOrEmpty(eventName) && !names.Events.Contains(eventName))
        {
            context.Warning(DiagnosticCodes.UnknownEvent, $"Unknown event '{eventName}' - declare it with 'event {eventName}'", handler.Location);
        }

        var mappings = handler switch { GraphFromSyntax from => from.Mappings, EverySyntax every => every.Mappings, _ => [] };
        foreach (var mapping in mappings.Where(mapping => properties.All(property => property.Name != mapping.Property)))
        {
            context.Error(DiagnosticCodes.UnknownGraphProperty, $"'{mapping.Property}' is not a declared property", mapping.Location);
        }
    }
}

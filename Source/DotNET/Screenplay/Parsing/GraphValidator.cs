// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates graph declarations, the graph projections that build them and the specification steps that assert them.
/// </summary>
internal static class GraphValidator
{
    internal static void Validate(ApplicationSyntax application, IReadOnlyList<SliceSyntax> slices, DocumentNames names, ParserContext context)
    {
        var graphs = slices.SelectMany(slice => slice.Graphs).ToList();
        foreach (var duplicate in graphs.GroupBy(graph => graph.Name, StringComparer.Ordinal).Where(group => group.Count() > 1).SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.DuplicateGraphElement, $"Duplicate graph '{duplicate.Name}' - a graph is declared once", duplicate.Location);
        }

        var concepts = application.Concepts.Select(concept => concept.Name).Concat(application.Imports.Select(import => import.Name)).ToHashSet(StringComparer.Ordinal);
        foreach (var graph in graphs)
        {
            ValidateDeclaration(graph, concepts, context);
        }

        var byName = graphs.GroupBy(graph => graph.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var projection in slices.SelectMany(slice => slice.GraphProjections))
        {
            GraphProjectionValidator.Validate(projection, byName, names, context);
        }

        var readModels = slices.SelectMany(slice => slice.ReadModels ?? []).GroupBy(readModel => readModel.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var types = (application.Types ?? []).GroupBy(type => type.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        foreach (var query in slices.SelectMany(slice => slice.GraphQueries))
        {
            GraphQueryValidator.Validate(query, byName, readModels, types, context);
        }

        foreach (var specification in slices.SelectMany(slice => slice.Specifications))
        {
            foreach (var step in specification.GivenGraphs.Concat(specification.ThenGraphs))
            {
                ValidateSpecification(step, byName, context);
            }
        }
    }

    static void ValidateDeclaration(GraphSyntax graph, HashSet<string> concepts, ParserContext context)
    {
        Duplicates(graph.Nodes.Select(node => (node.Label, node.Location)), "node", graph.Name, context);
        Duplicates(graph.Edges.Select(edge => (edge.Type, edge.Location)), "edge", graph.Name, context);
        var labels = graph.Nodes.Select(node => node.Label).ToHashSet(StringComparer.Ordinal);

        foreach (var node in graph.Nodes)
        {
            Duplicates(node.Properties.Select(property => (property.Name, property.Location)), $"property of node '{node.Label}'", graph.Name, context);
            ValidateTypes(node.Properties, concepts, context);
            var keys = node.Properties.Count(property => property.IsKey);
            if (keys != 1 && node.Label.Length > 0)
            {
                context.Error(DiagnosticCodes.InvalidGraphNodeKey, $"Node '{node.Label}' of graph '{graph.Name}' declares {(keys == 0 ? "no key property" : $"{keys} key properties")} - mark exactly one property 'key'", node.Location);
            }
        }

        foreach (var edge in graph.Edges.Where(edge => edge.Type.Length > 0))
        {
            Duplicates(edge.Properties.Select(property => (property.Name, property.Location)), $"property of edge '{edge.Type}'", graph.Name, context);
            ValidateTypes(edge.Properties, concepts, context);
            foreach (var (role, label) in new[] { ("source", edge.Source), ("target", edge.Target) }.Where(end => !labels.Contains(end.Item2)))
            {
                context.Error(DiagnosticCodes.UnknownGraphEdgeEndpoint, $"Edge '{edge.Type}' of graph '{graph.Name}' has {role} '{label}', which the graph does not declare as a node", edge.Location);
            }

            if (edge.IdentifiedBy is { } identity && edge.Properties.All(property => property.Name != identity))
            {
                context.Error(DiagnosticCodes.UnknownGraphEdgeIdentity, $"Edge '{edge.Type}' is identified by '{identity}', which it does not declare as a property", edge.Location);
            }
        }
    }

    static void Duplicates(IEnumerable<(string Name, SourceLocation Location)> items, string what, string graph, ParserContext context)
    {
        foreach (var duplicate in items.Where(item => item.Name.Length > 0).GroupBy(item => item.Name, StringComparer.Ordinal).Where(group => group.Count() > 1).SelectMany(group => group.Skip(1)))
        {
            context.Error(DiagnosticCodes.DuplicateGraphElement, $"Duplicate {what} '{duplicate.Name}' in graph '{graph}'", duplicate.Location);
        }
    }

    static void ValidateTypes(IEnumerable<PropertySyntax> properties, HashSet<string> concepts, ParserContext context)
    {
        foreach (var property in properties.Where(property => !ConceptSyntax.PrimitiveTypes.Contains(property.Type.Name) && !concepts.Contains(property.Type.Name)))
        {
            context.Error(
                DiagnosticCodes.InvalidGraphPropertyType,
                $"Graph property '{property.Name}' has type '{property.Type.Name}', which is not a primitive or a concept over one - a graph property holds a primitive or an array of primitives",
                property.Type.Location);
        }
    }

    static void ValidateSpecification(SpecificationGraphSyntax step, Dictionary<string, GraphSyntax> graphs, ParserContext context)
    {
        if (!graphs.TryGetValue(step.Graph, out var graph))
        {
            context.Error(DiagnosticCodes.UnknownGraphSpecificationReference, $"Unknown graph '{step.Graph}' - declare it with 'graph {step.Graph}'", step.Location);
            return;
        }

        foreach (var element in step.Elements)
        {
            var declared = element switch
            {
                SpecificationGraphNodeSyntax node => graph.Nodes.FirstOrDefault(candidate => candidate.Label == node.Label)?.Properties.AsEnumerable(),
                SpecificationGraphEdgeSyntax edge => graph.Edges.FirstOrDefault(candidate => candidate.Type == edge.Type)?.Properties.AsEnumerable(),
                _ => null
            };
            if (declared is null)
            {
                var name = element is SpecificationGraphNodeSyntax n ? $"node '{n.Label}'" : $"edge '{((SpecificationGraphEdgeSyntax)element).Type}'";
                context.Error(DiagnosticCodes.UnknownGraphSpecificationReference, $"Graph '{graph.Name}' does not declare {name}", element.Location);
                continue;
            }

            foreach (var value in element.Properties.Where(value => declared.All(property => property.Name != value.Property)))
            {
                context.Error(DiagnosticCodes.UnknownGraphProperty, $"'{value.Property}' is not a declared property of {(element is SpecificationGraphNodeSyntax ? "the node" : "the edge")}", value.Location);
            }
        }
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    // ESM v38 constructs: property graphs, the projections that build them and the specifications that assert them.
    internal static bool UsesGraphs(SemanticApplication application) =>
        !application.Graphs.IsDefaultOrEmpty || !application.GraphProjections.IsDefaultOrEmpty || !application.GraphSpecifications.IsDefaultOrEmpty;

    // ESM v40 constructs: queries that read a graph.
    internal static bool UsesGraphQueries(SemanticApplication application) =>
        !application.GraphQueries.IsDefaultOrEmpty || application.GraphSpecifications.Any(specification => !specification.ThenQueries.IsDefaultOrEmpty || specification.Clock is not null);

    static void ValidateGraphQueryVersion(SemanticApplication application, SemanticVersion version)
    {
        ValidateFeatureVersion(
            UsesGraphQueries(application),
            version,
            SemanticVersion.V40,
            "An ESM v40 model must declare a graph query.",
            "Graph queries require ESM v40.");
        if (!version.IsAtLeast(SemanticVersion.V40)) return;
        RejectGraphDuplicates(application.GraphQueries.Select(_ => _.Name), "graph query");
        var graphs = application.Graphs.ToDictionary(graph => graph.Name, StringComparer.Ordinal);
        var readModels = SlicesOf(application).SelectMany(slice => slice.ReadModels).ToDictionary(readModel => readModel.Id);
        foreach (var query in application.GraphQueries)
        {
            if (!graphs.TryGetValue(query.Graph, out var graph)) throw new InvalidSemanticContract($"Graph query '{query.Name}' names unknown graph '{query.Graph}'.");
            if (!readModels.TryGetValue(query.ReadModel, out var readModel)) throw new InvalidSemanticContract($"Graph query '{query.Name}' returns unknown read model '{query.ReadModel}'.");
            if (query.Matches.IsDefaultOrEmpty || query.Matches.All(match => match.IsOptional)) throw new InvalidSemanticContract($"Graph query '{query.Name}' has no required match.");
            foreach (var @return in query.Returns)
            {
                if (readModel.Properties.All(property => property.Id != @return.Property)) throw new InvalidSemanticContract($"Graph query '{query.Name}' returns a property read model '{readModel.Name}' does not have.");
            }

            foreach (var condition in query.Conditions)
            {
                if ((condition.Argument is null) == (condition.Literal is null)) throw new InvalidSemanticContract($"Graph query '{query.Name}' condition on '{condition.Alias}.{condition.Property}' must compare with exactly one of an argument and a literal.");
                if (condition.Argument is not null && query.Arguments.All(argument => argument.Name != condition.Argument)) throw new InvalidSemanticContract($"Graph query '{query.Name}' uses undeclared argument '{condition.Argument}'.");
            }
        }
    }

    static void ValidateGraphVersion(SemanticApplication application, SemanticVersion version)
    {
        ValidateFeatureVersion(
            UsesGraphs(application),
            version,
            SemanticVersion.V38,
            "An ESM v38 model must declare a graph.",
            "Graphs, graph projections and graph specifications require ESM v38.");
        if (!version.IsAtLeast(SemanticVersion.V38)) return;
        ValidateGraphGraph(application);
    }

    static void ValidateGraphGraph(SemanticApplication application)
    {
        RejectGraphDuplicates(application.Graphs.Select(_ => _.Name), "graph");
        RejectGraphDuplicates(application.GraphProjections.Select(_ => _.Name), "graph projection");
        var graphs = application.Graphs.ToDictionary(graph => graph.Name, StringComparer.Ordinal);
        foreach (var graph in application.Graphs) ValidateGraph(graph);
        var events = SlicesOf(application).SelectMany(slice => slice.Events).Select(@event => @event.Id).ToHashSet();
        foreach (var projection in application.GraphProjections) ValidateGraphProjection(projection, graphs, events);
        foreach (var specification in application.GraphSpecifications)
        {
            foreach (var step in specification.GivenGraphs.Concat(specification.ThenGraphs))
            {
                if (!graphs.TryGetValue(step.Graph, out var graph)) throw new InvalidSemanticContract($"Graph specification '{specification.Name}' names unknown graph '{step.Graph}'.");
                foreach (var element in step.Elements) ValidateGraphStepElement(specification, graph, element);
            }
        }
    }

    static void ValidateGraph(SemanticGraph graph)
    {
        if (graph.Nodes.IsDefaultOrEmpty) throw new InvalidSemanticContract($"Graph '{graph.Name}' must declare at least one node.");
        RejectGraphDuplicates(graph.Nodes.Select(_ => _.Label), $"node of graph '{graph.Name}'");
        RejectGraphDuplicates(graph.Edges.Select(_ => _.Type), $"edge of graph '{graph.Name}'");
        foreach (var node in graph.Nodes)
        {
            RejectGraphDuplicates(node.Properties.Select(_ => _.Name), $"property of node '{node.Label}'");
            if (node.Properties.Count(property => property.IsKey) != 1) throw new InvalidSemanticContract($"Node '{node.Label}' of graph '{graph.Name}' must have exactly one key property.");
            foreach (var key in node.Properties.Where(property => property.IsKey))
            {
                if (key.Type.IsCollection || key.Type.IsOptional) throw new InvalidSemanticContract($"The key of node '{node.Label}' must be a required single value.");
            }

            RejectNonPrimitive(node.Properties, node.Label);
        }

        foreach (var edge in graph.Edges)
        {
            RejectGraphDuplicates(edge.Properties.Select(_ => _.Name), $"property of edge '{edge.Type}'");
            if (edge.Properties.Any(property => property.IsKey)) throw new InvalidSemanticContract($"Edge '{edge.Type}' cannot have a key property.");
            if (graph.Nodes.All(node => node.Label != edge.Source) || graph.Nodes.All(node => node.Label != edge.Target)) throw new InvalidSemanticContract($"Edge '{edge.Type}' of graph '{graph.Name}' joins a label the graph does not declare.");
            if (edge.IdentifiedBy is not null && edge.Properties.All(property => property.Name != edge.IdentifiedBy)) throw new InvalidSemanticContract($"Edge '{edge.Type}' is identified by a property it does not declare.");
            RejectNonPrimitive(edge.Properties, edge.Type);
        }
    }

    static void RejectNonPrimitive(ImmutableArray<SemanticGraphProperty> properties, string owner)
    {
        if (properties.Any(property => property.Type.Kind == SemanticTypeReferenceKind.CompositeType)) throw new InvalidSemanticContract($"A property of '{owner}' is a composite type; a graph property holds a primitive, a concept or an array of those.");
    }

    static void ValidateGraphProjection(SemanticGraphProjection projection, Dictionary<string, SemanticGraph> graphs, HashSet<SemanticId> events)
    {
        if (!graphs.TryGetValue(projection.Graph, out var graph)) throw new InvalidSemanticContract($"Graph projection '{projection.Name}' builds unknown graph '{projection.Graph}'.");
        RejectGraphDuplicates(projection.Nodes.Select(_ => _.Name), $"node block of graph projection '{projection.Name}'");
        RejectGraphDuplicates(projection.Edges.Select(_ => _.Name), $"edge block of graph projection '{projection.Name}'");
        foreach (var node in projection.Nodes)
        {
            var declared = graph.Nodes.FirstOrDefault(candidate => candidate.Label == node.Name) ?? throw new InvalidSemanticContract($"Graph projection '{projection.Name}' builds undeclared node '{node.Name}'.");
            foreach (var handler in node.Handlers)
            {
                if (!events.Contains(handler.Event)) throw new InvalidSemanticContract($"Graph projection '{projection.Name}' observes an unknown event.");
                if (handler.Key is null || handler.Source is not null || handler.Target is not null || handler.Identity is not null) throw new InvalidSemanticContract($"A handler of node '{node.Name}' is identified by exactly a key.");
                if (handler.Removes && !handler.Mappings.IsDefaultOrEmpty) throw new InvalidSemanticContract($"A removal of node '{node.Name}' has no mappings.");
                ValidateGraphMappings(handler, declared.Properties, node.Name);
            }
        }

        foreach (var edge in projection.Edges)
        {
            var declared = graph.Edges.FirstOrDefault(candidate => candidate.Type == edge.Name) ?? throw new InvalidSemanticContract($"Graph projection '{projection.Name}' builds undeclared edge '{edge.Name}'.");
            foreach (var handler in edge.Handlers)
            {
                if (!events.Contains(handler.Event)) throw new InvalidSemanticContract($"Graph projection '{projection.Name}' observes an unknown event.");
                if (handler.Key is not null || handler.Source is null || handler.Target is null) throw new InvalidSemanticContract($"A handler of edge '{edge.Name}' is identified by a source and a target.");
                if ((handler.Identity is null) != (declared.IdentifiedBy is null)) throw new InvalidSemanticContract($"A handler of edge '{edge.Name}' must supply an identity exactly when the edge is identified by a property.");
                if (handler.Detach) throw new InvalidSemanticContract($"A removal of edge '{edge.Name}' cannot detach.");
                if (handler.Removes && !handler.Mappings.IsDefaultOrEmpty) throw new InvalidSemanticContract($"A removal of edge '{edge.Name}' has no mappings.");
                ValidateGraphMappings(handler, declared.Properties, edge.Name);
            }
        }
    }

    static void ValidateGraphMappings(SemanticGraphHandler handler, ImmutableArray<SemanticGraphProperty> properties, string owner)
    {
        if (handler.Detach && !handler.Removes) throw new InvalidSemanticContract($"Only a removal of '{owner}' can detach.");
        foreach (var mapping in handler.Mappings)
        {
            if (properties.All(property => property.Name != mapping.Property)) throw new InvalidSemanticContract($"A mapping of '{owner}' sets undeclared property '{mapping.Property}'.");
            var needsSource = mapping.Operation is SemanticProjectionOperation.Set or SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract;
            if (needsSource != (mapping.Source is not null)) throw new InvalidSemanticContract($"The mapping of '{mapping.Property}' of '{owner}' has the wrong source for its operation.");
            if (mapping.Factor is not null && mapping.Operation is not (SemanticProjectionOperation.Add or SemanticProjectionOperation.Subtract)) throw new InvalidSemanticContract($"Only an add or subtract mapping of '{owner}' takes a factor.");
        }
    }

    static void ValidateGraphStepElement(SemanticGraphSpecification specification, SemanticGraph graph, SemanticGraphElementStep element)
    {
        if (element.IsNode)
        {
            var node = graph.Nodes.FirstOrDefault(candidate => candidate.Label == element.Name) ?? throw new InvalidSemanticContract($"Graph specification '{specification.Name}' names undeclared node '{element.Name}'.");
            if (element.Target is not null || element.Identity is not null) throw new InvalidSemanticContract("A node step has no target or identity.");
            RejectUnknownStepValues(specification, element, node.Properties);
        }
        else
        {
            var edge = graph.Edges.FirstOrDefault(candidate => candidate.Type == element.Name) ?? throw new InvalidSemanticContract($"Graph specification '{specification.Name}' names undeclared edge '{element.Name}'.");
            if (element.Target is null) throw new InvalidSemanticContract("An edge step has a target.");
            if ((element.Identity is null) != (edge.IdentifiedBy is null)) throw new InvalidSemanticContract("An edge step supplies an identity exactly when the edge is identified by a property.");
            RejectUnknownStepValues(specification, element, edge.Properties);
        }

        if (element.Absent && !element.Values.IsDefaultOrEmpty) throw new InvalidSemanticContract("An absence step has no property values.");
    }

    static void RejectUnknownStepValues(SemanticGraphSpecification specification, SemanticGraphElementStep element, ImmutableArray<SemanticGraphProperty> properties)
    {
        foreach (var value in element.Values)
        {
            if (properties.All(property => property.Name != value.Property)) throw new InvalidSemanticContract($"Graph specification '{specification.Name}' states undeclared property '{value.Property}'.");
        }
    }

    static void RejectGraphDuplicates(IEnumerable<string> names, string what)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (!seen.Add(name)) throw new InvalidSemanticContract($"Duplicate {what} '{name}'.");
        }
    }
}

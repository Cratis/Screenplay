// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents the verdict of one graph specification.
/// </summary>
/// <param name="Owner">The name of the slice the specification belongs to.</param>
/// <param name="Name">The specification name.</param>
/// <param name="Failures">What did not hold; empty when the specification passed.</param>
public sealed record SemanticGraphSpecificationResult(string Owner, string Name, ImmutableArray<string> Failures)
{
    /// <summary>
    /// Gets whether the specification passed.
    /// </summary>
    public bool Passed => Failures.IsEmpty;
}

/// <summary>
/// Runs graph specifications in the reference evaluator: an in-memory property graph that applies the five graph operations.
/// </summary>
/// <remarks>
/// A graph projection yields idempotent operations: upsert a node, set properties, delete a node (with or without its edges),
/// upsert an edge and delete an edge. An edge operation ensures its endpoint nodes exist. A node is identified by its label and key;
/// an edge by its type, its source and target keys, and the identifying property value when the edge declares one. Values read from
/// <c>$eventContext</c> are not materialized by the reference evaluator, so a property mapped from them holds no value.
/// </remarks>
public static class SemanticGraphRunner
{
    /// <summary>The instant events occur at when a specification states no <c>given clock</c>.</summary>
    internal const string DefaultClock = "2000-01-01T00:00:00.0000000Z";

    /// <summary>
    /// Runs every graph specification of a model.
    /// </summary>
    /// <param name="model">The validated executable semantic model.</param>
    /// <returns>One result per specification.</returns>
    public static ImmutableArray<SemanticGraphSpecificationResult> RunAll(ExecutableSemanticModel model)
    {
        var application = model.Application;
        if (application.GraphSpecifications.IsDefaultOrEmpty) return [];
        return [.. application.GraphSpecifications.Select(specification => new SemanticGraphSpecificationResult(specification.Owner, specification.Name, Run(application, specification)))];
    }

    /// <summary>Gets the comparable text of a value, as the runner compares values.</summary>
    /// <param name="value">The value.</param>
    /// <returns>Its canonical text, or null for no value.</returns>
    internal static string? Canonical(SemanticValue? value) => Canon(value);

    static ImmutableArray<string> Run(SemanticApplication application, SemanticGraphSpecification specification)
    {
        var graphs = application.Graphs.ToDictionary(graph => graph.Name, StringComparer.Ordinal);
        var state = new GraphState(graphs);
        foreach (var step in specification.GivenGraphs) Seed(state, step);
        foreach (var @event in specification.Given.Concat(specification.WhenAppended is null ? [] : [specification.WhenAppended]))
        {
            foreach (var projection in application.GraphProjections) Apply(state, graphs[projection.Graph], projection, @event, specification.Clock ?? DefaultClock);
        }

        var failures = ImmutableArray.CreateBuilder<string>();
        foreach (var step in specification.ThenGraphs)
        {
            foreach (var element in step.Elements) Assert(state, step.Graph, element, failures);
        }

        var readModels = application.Modules.SelectMany(SemanticGraphQueryRunner.SlicesOf).SelectMany(slice => slice.ReadModels).ToDictionary(readModel => readModel.Id);
        foreach (var expectation in specification.ThenQueries)
        {
            SemanticGraphQueryRunner.Assert(application, readModels, state.Snapshot(), expectation, failures);
        }

        return failures.ToImmutable();
    }

    static void Seed(GraphState state, SemanticGraphStep step)
    {
        foreach (var element in step.Elements)
        {
            if (element.IsNode)
            {
                var node = state.UpsertNode(step.Graph, element.Name, Canon(element.Key));
                foreach (var value in element.Values) node[value.Property] = value.Value;
            }
            else
            {
                var edge = state.UpsertEdge(step.Graph, element.Name, Canon(element.Key), Canon(element.Target), element.Identity is null ? null : Canon(element.Identity));
                foreach (var value in element.Values) edge[value.Property] = value.Value;
            }
        }
    }

    static void Apply(GraphState state, SemanticGraph graph, SemanticGraphProjection projection, SemanticSpecificationEvent appended, string clock)
    {
        foreach (var node in projection.Nodes)
        {
            var declared = graph.Nodes.First(candidate => candidate.Label == node.Name);
            foreach (var handler in node.Handlers.Where(handler => handler.Event == appended.EventContract))
            {
                foreach (var @event in Occurrences(handler, appended))
                {
                    if (Canon(Evaluate(handler.Key, @event, clock)) is not { } key) continue;
                    if (handler.Removes)
                    {
                        state.DeleteNode(graph.Name, node.Name, key, handler.Detach);
                        continue;
                    }

                    var properties = state.UpsertNode(graph.Name, node.Name, key);
                    properties[declared.Properties.First(property => property.IsKey).Name] = Evaluate(handler.Key, @event, clock)!;
                    ApplyMappings(properties, handler, @event, clock);
                }
            }
        }

        foreach (var edge in projection.Edges)
        {
            foreach (var handler in edge.Handlers.Where(handler => handler.Event == appended.EventContract))
            {
                foreach (var @event in Occurrences(handler, appended))
                {
                    if (Canon(Evaluate(handler.Source, @event, clock)) is not { } source || Canon(Evaluate(handler.Target, @event, clock)) is not { } target) continue;
                    var identity = Canon(Evaluate(handler.Identity, @event, clock));
                    if (handler.Removes)
                    {
                        state.DeleteEdge(graph.Name, edge.Name, source, target, identity);
                        continue;
                    }

                    ApplyMappings(state.UpsertEdge(graph.Name, edge.Name, source, target, identity), handler, @event, clock);
                }
            }
        }
    }

    // A handler that iterates a collection runs once per item, on the event with the collection replaced by that item - so an
    // expression written as slots.series resolves to the item's series, exactly as it does in the kernel. An absent or empty collection yields nothing.
    static IEnumerable<SemanticSpecificationEvent> Occurrences(SemanticGraphHandler handler, SemanticSpecificationEvent appended)
    {
        if (handler.ForEach is not { } collection)
        {
            yield return appended;
            yield break;
        }

        if (appended.Values.FirstOrDefault(value => value.TargetProperty == collection)?.Value is not SemanticArrayValue items) yield break;
        foreach (var item in items.Values)
        {
            yield return appended with { Values = [.. appended.Values.Select(value => value.TargetProperty == collection ? value with { Value = item } : value)] };
        }
    }

    static void ApplyMappings(Dictionary<string, SemanticValue> properties, SemanticGraphHandler handler, SemanticSpecificationEvent @event, string clock)
    {
        foreach (var mapping in handler.Mappings)
        {
            var source = Evaluate(mapping.Source, @event, clock);
            var factor = mapping.Factor is null ? 1m : Number(Evaluate(mapping.Factor, @event, clock)) ?? 1m;
            var current = properties.TryGetValue(mapping.Property, out var existing) ? Number(existing) ?? 0m : 0m;
            switch (mapping.Operation)
            {
                case SemanticProjectionOperation.Set when source is not null and not SemanticNullValue:
                    properties[mapping.Property] = source;
                    break;
                case SemanticProjectionOperation.Set or SemanticProjectionOperation.Clear:
                    properties.Remove(mapping.Property);
                    break;
                case SemanticProjectionOperation.Increment:
                    properties[mapping.Property] = SemanticValue.Number(current + 1);
                    break;
                case SemanticProjectionOperation.Decrement:
                    properties[mapping.Property] = SemanticValue.Number(current - 1);
                    break;
                case SemanticProjectionOperation.Add when Number(source) is { } amount:
                    properties[mapping.Property] = SemanticValue.Number(current + (amount * factor));
                    break;
                case SemanticProjectionOperation.Subtract when Number(source) is { } amount:
                    properties[mapping.Property] = SemanticValue.Number(current - (amount * factor));
                    break;
            }
        }
    }

    static SemanticValue? Evaluate(SemanticProjectionValue? value, SemanticSpecificationEvent @event, string clock) => value switch
    {
        SemanticProjectionLiteral literal => literal.Value,
        SemanticProjectionEventSourceIdentity => @event.EventSource?.Value,
        SemanticProjectionEventProperty property => EventProperty(property.Path, @event),
        SemanticProjectionEventContextValue { Path: "occurred" } => SemanticValue.Text(clock),
        SemanticProjectionEventContextValue { Path: "eventSourceId" } => @event.EventSource?.Value,
        _ => null
    };

    static SemanticValue? EventProperty(ImmutableArray<SemanticId> path, SemanticSpecificationEvent @event)
    {
        if (path.IsDefaultOrEmpty) return null;
        var current = @event.Values.FirstOrDefault(value => value.TargetProperty == path[0])?.Value;
        foreach (var segment in path.Skip(1))
        {
            current = (current as SemanticCompositeValue)?.Properties.FirstOrDefault(value => value.TargetProperty == segment)?.Value;
        }

        return current;
    }

    static void Assert(GraphState state, string graph, SemanticGraphElementStep element, ImmutableArray<string>.Builder failures)
    {
        var description = element.IsNode
            ? $"node {element.Name} {Canon(element.Key)}"
            : $"edge {element.Name} from {Canon(element.Key)} to {Canon(element.Target)}{(element.Identity is null ? string.Empty : $" identified by {Canon(element.Identity)}")}";
        var actual = element.IsNode
            ? state.Node(graph, element.Name, Canon(element.Key))
            : state.Edge(graph, element.Name, Canon(element.Key), Canon(element.Target), element.Identity is null ? null : Canon(element.Identity));
        if (element.Absent)
        {
            if (actual is not null) failures.Add($"Expected no {description}, but it exists.");
            return;
        }

        if (actual is null)
        {
            failures.Add($"Expected {description}, but it does not exist.");
            return;
        }

        foreach (var value in element.Values)
        {
            if (!actual.TryGetValue(value.Property, out var held)) failures.Add($"Expected {description} to have {value.Property} = {Canon(value.Value)}, but it holds no value.");
            else if (Canon(held) != Canon(value.Value)) failures.Add($"Expected {description} to have {value.Property} = {Canon(value.Value)}, but it holds {Canon(held)}.");
        }
    }

    static decimal? Number(SemanticValue? value) => value switch
    {
        SemanticNumberValue number => number.Value,
        SemanticTextValue text when decimal.TryParse(text.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) => parsed,
        _ => null
    };

    static string? Canon(SemanticValue? value) => value switch
    {
        null => null,
        SemanticTextValue text => Guid.TryParse(text.Value, out var guid) ? guid.ToString("D", CultureInfo.InvariantCulture) : text.Value,
        SemanticNumberValue number => number.Value.ToString("G29", CultureInfo.InvariantCulture),
        SemanticBooleanValue boolean => boolean.Value ? "true" : "false",
        SemanticNullValue => "null",
        SemanticArrayValue array => $"[{string.Join(", ", array.Values.Select(Canon))}]",
        _ => value.ToString()
    };

    internal sealed class GraphSnapshot(Dictionary<(string Graph, string Label, string Key), Dictionary<string, SemanticValue>> nodes, Dictionary<(string Graph, string Type, string Source, string Target, string? Identity), Dictionary<string, SemanticValue>> edges)
    {
        public IEnumerable<(string Key, Dictionary<string, SemanticValue> Properties)> Nodes(string graph, string label) =>
            nodes.Where(node => node.Key.Graph == graph && node.Key.Label == label).Select(node => (node.Key.Key, node.Value));

        public IEnumerable<(string Source, string Target, Dictionary<string, SemanticValue> Properties)> Edges(string graph, string type) =>
            edges.Where(edge => edge.Key.Graph == graph && edge.Key.Type == type).Select(edge => (edge.Key.Source, edge.Key.Target, edge.Value));

        public IEnumerable<(string Source, string Target, string? Identity, Dictionary<string, SemanticValue> Properties)> IdentifiedEdges(string graph, string type) =>
            edges.Where(edge => edge.Key.Graph == graph && edge.Key.Type == type).Select(edge => (edge.Key.Source, edge.Key.Target, edge.Key.Identity, edge.Value));
    }

    sealed class GraphState(Dictionary<string, SemanticGraph> graphs)
    {
        readonly Dictionary<(string Graph, string Label, string Key), Dictionary<string, SemanticValue>> _nodes = [];
        readonly Dictionary<(string Graph, string Type, string Source, string Target, string? Identity), Dictionary<string, SemanticValue>> _edges = [];

        public GraphSnapshot Snapshot() => new(_nodes, _edges);

        public Dictionary<string, SemanticValue>? Node(string graph, string label, string? key) => key is not null && _nodes.TryGetValue((graph, label, key), out var node) ? node : null;

        public Dictionary<string, SemanticValue>? Edge(string graph, string type, string? source, string? target, string? identity) =>
            source is not null && target is not null && _edges.TryGetValue((graph, type, source, target, identity), out var edge) ? edge : null;

        public Dictionary<string, SemanticValue> UpsertNode(string graph, string label, string? key)
        {
            var identity = (graph, label, key!);
            if (!_nodes.TryGetValue(identity, out var node))
            {
                _nodes[identity] = node = [];
                if (key is not null && graphs[graph].Nodes.First(candidate => candidate.Label == label).Properties.FirstOrDefault(property => property.IsKey) is { } keyProperty)
                {
                    node[keyProperty.Name] = SemanticValue.Text(key);
                }
            }

            return node;
        }

        public Dictionary<string, SemanticValue> UpsertEdge(string graph, string type, string? source, string? target, string? identity)
        {
            var declared = graphs[graph].Edges.First(edge => edge.Type == type);
            UpsertNode(graph, declared.Source, source);
            UpsertNode(graph, declared.Target, target);
            var key = (graph, type, source!, target!, identity);
            if (!_edges.TryGetValue(key, out var edge)) _edges[key] = edge = [];
            return edge;
        }

        public void DeleteNode(string graph, string label, string key, bool detach)
        {
            _nodes.Remove((graph, label, key));
            if (!detach) return;
            foreach (var edge in _edges.Keys.Where(candidate => candidate.Graph == graph).ToArray())
            {
                var declared = graphs[graph].Edges.First(candidate => candidate.Type == edge.Type);
                if ((declared.Source == label && edge.Source == key) || (declared.Target == label && edge.Target == key)) _edges.Remove(edge);
            }
        }

        public void DeleteEdge(string graph, string type, string source, string target, string? identity) => _edges.Remove((graph, type, source, target, identity));
    }
}

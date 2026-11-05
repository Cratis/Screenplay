// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents a property of a graph node or edge.
/// </summary>
/// <param name="Name">The property name.</param>
/// <param name="Type">The primitive, concept or array type of the property.</param>
/// <param name="IsKey">Whether the property is the key of its node.</param>
public sealed record SemanticGraphProperty(string Name, SemanticTypeReference Type, bool IsKey);

/// <summary>
/// Represents a node label of a graph: its properties, exactly one of them the key.
/// </summary>
/// <param name="Label">The node label.</param>
/// <param name="Properties">The properties of the node.</param>
public sealed record SemanticGraphNode(string Label, ImmutableArray<SemanticGraphProperty> Properties);

/// <summary>
/// Represents a directed edge type of a graph between two node labels.
/// </summary>
/// <param name="Type">The edge type.</param>
/// <param name="Source">The label of the source node.</param>
/// <param name="Target">The label of the target node.</param>
/// <param name="Properties">The properties of the edge.</param>
public sealed record SemanticGraphEdge(string Type, string Source, string Target, ImmutableArray<SemanticGraphProperty> Properties)
{
    /// <summary>
    /// Gets the property that tells edges of the same type between the same two nodes apart, or null when the edge is identified by its endpoints alone.
    /// </summary>
    public string? IdentifiedBy { get; init; }
}

/// <summary>
/// Represents the declared shape of a property graph: derived state that projections build and nobody writes to.
/// </summary>
/// <param name="Name">The graph name.</param>
/// <param name="Nodes">The node labels.</param>
/// <param name="Edges">The edge types.</param>
public sealed record SemanticGraph(string Name, ImmutableArray<SemanticGraphNode> Nodes, ImmutableArray<SemanticGraphEdge> Edges);

/// <summary>
/// Represents one property mapping of a graph handler.
/// </summary>
/// <param name="Property">The node or edge property the mapping changes.</param>
/// <param name="Operation">How the property changes.</param>
/// <param name="Source">The value the mapping reads, or null for a clear, increment or decrement.</param>
public sealed record SemanticGraphMapping(string Property, SemanticProjectionOperation Operation, SemanticProjectionValue? Source)
{
    /// <summary>
    /// Gets the optional factor an add or subtract multiplies the source by.
    /// </summary>
    public SemanticProjectionValue? Factor { get; init; }
}

/// <summary>
/// Represents how one event creates, changes or removes a node or an edge.
/// </summary>
/// <param name="Event">The event contract the handler observes.</param>
/// <param name="Mappings">The property mappings, applied in order.</param>
public sealed record SemanticGraphHandler(SemanticId Event, ImmutableArray<SemanticGraphMapping> Mappings)
{
    /// <summary>
    /// Gets the value of the node key; set on a node handler.
    /// </summary>
    public SemanticProjectionValue? Key { get; init; }

    /// <summary>
    /// Gets the value of the source node key; set on an edge handler.
    /// </summary>
    public SemanticProjectionValue? Source { get; init; }

    /// <summary>
    /// Gets the value of the target node key; set on an edge handler.
    /// </summary>
    public SemanticProjectionValue? Target { get; init; }

    /// <summary>
    /// Gets the value of the identifying edge property; set when the edge declares one.
    /// </summary>
    public SemanticProjectionValue? Identity { get; init; }

    /// <summary>
    /// Gets whether the handler removes the node or edge instead of creating or changing it.
    /// </summary>
    public bool Removes { get; init; }

    /// <summary>
    /// Gets whether a node removal also removes the edges attached to the node.
    /// </summary>
    public bool Detach { get; init; }
}

/// <summary>
/// Represents the handlers of one node label or edge type of a graph projection.
/// </summary>
/// <param name="Name">The node label or edge type.</param>
/// <param name="Handlers">The handlers.</param>
public sealed record SemanticGraphElementHandlers(string Name, ImmutableArray<SemanticGraphHandler> Handlers);

/// <summary>
/// Represents a projection that builds a graph from events.
/// </summary>
/// <param name="Name">The projection name.</param>
/// <param name="Graph">The name of the graph the projection builds.</param>
/// <param name="Nodes">The node handlers by label.</param>
/// <param name="Edges">The edge handlers by type.</param>
public sealed record SemanticGraphProjection(string Name, string Graph, ImmutableArray<SemanticGraphElementHandlers> Nodes, ImmutableArray<SemanticGraphElementHandlers> Edges);

/// <summary>
/// Represents a property value a graph specification states.
/// </summary>
/// <param name="Property">The property name.</param>
/// <param name="Value">The value.</param>
public sealed record SemanticGraphPropertyValue(string Property, SemanticValue Value);

/// <summary>
/// Represents a node or an edge a graph specification step states.
/// </summary>
/// <param name="IsNode">Whether the element is a node rather than an edge.</param>
/// <param name="Absent">Whether the step asserts the element does not exist.</param>
/// <param name="Name">The node label or edge type.</param>
/// <param name="Key">The node key, or the source node key of an edge.</param>
/// <param name="Values">The property values the step states.</param>
public sealed record SemanticGraphElementStep(bool IsNode, bool Absent, string Name, SemanticValue Key, ImmutableArray<SemanticGraphPropertyValue> Values)
{
    /// <summary>
    /// Gets the target node key of an edge.
    /// </summary>
    public SemanticValue? Target { get; init; }

    /// <summary>
    /// Gets the value of the identifying edge property.
    /// </summary>
    public SemanticValue? Identity { get; init; }
}

/// <summary>
/// Represents a <c>given graph</c> or <c>then graph</c> step.
/// </summary>
/// <param name="Graph">The name of the graph.</param>
/// <param name="Elements">The node and edge steps.</param>
public sealed record SemanticGraphStep(string Graph, ImmutableArray<SemanticGraphElementStep> Elements);

/// <summary>
/// Represents a specification that seeds a graph, appends events and asserts the graph the graph projections leave behind.
/// </summary>
/// <param name="Owner">The name of the slice the specification belongs to.</param>
/// <param name="Name">The specification name.</param>
/// <param name="Given">The events that happened before.</param>
public sealed record SemanticGraphSpecification(string Owner, string Name, ImmutableArray<SemanticSpecificationEvent> Given)
{
    /// <summary>
    /// Gets the graphs seeded before the events are applied.
    /// </summary>
    public ImmutableArray<SemanticGraphStep> GivenGraphs { get; init; } = [];

    /// <summary>
    /// Gets the event the specification appends.
    /// </summary>
    public SemanticSpecificationEvent? WhenAppended { get; init; }

    /// <summary>
    /// Gets the graph contents asserted afterwards.
    /// </summary>
    public ImmutableArray<SemanticGraphStep> ThenGraphs { get; init; } = [];
}

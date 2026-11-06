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
    /// Gets the graph queries the specification runs and the rows it expects (ESM v40).
    /// </summary>
    public ImmutableArray<SemanticGraphQueryExpectation> ThenQueries { get; init; } = [];

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

/// <summary>
/// Represents an argument of a graph query: a caller-supplied value the conditions compare against.
/// </summary>
/// <param name="Name">The argument name.</param>
/// <param name="Type">The argument type.</param>
/// <param name="IsKey">Whether it was declared with <c language="screenplay">by</c> rather than <c language="screenplay">filter</c>.</param>
public sealed record SemanticGraphQueryArgument(string Name, SemanticTypeReference Type, bool IsKey);

/// <summary>
/// Represents a node in a path pattern.
/// </summary>
/// <param name="Alias">The alias, or null.</param>
/// <param name="Label">The node label.</param>
public sealed record SemanticGraphPatternNode(string? Alias, string Label);

/// <summary>
/// Represents an edge and the node it leads to in a path pattern.
/// </summary>
/// <param name="Alias">The edge alias, or null.</param>
/// <param name="Type">The edge type.</param>
/// <param name="Outgoing">Whether the edge points away from the previous node.</param>
/// <param name="Node">The node it leads to.</param>
public sealed record SemanticGraphHop(string? Alias, string Type, bool Outgoing, SemanticGraphPatternNode Node);

/// <summary>
/// Represents one path pattern of a graph query.
/// </summary>
/// <param name="IsOptional">Whether the path may be absent from a row.</param>
/// <param name="Start">The first node.</param>
/// <param name="Hops">The hops that follow it.</param>
public sealed record SemanticGraphMatch(bool IsOptional, SemanticGraphPatternNode Start, ImmutableArray<SemanticGraphHop> Hops);

/// <summary>
/// Represents an equality condition between a property of a matched node or edge and an argument or a literal.
/// </summary>
/// <param name="Alias">The alias of the node or edge.</param>
/// <param name="Property">Its property.</param>
public sealed record SemanticGraphCondition(string Alias, string Property)
{
    /// <summary>Gets the argument the property is compared with, or null when it is compared with <see cref="Literal"/>.</summary>
    public string? Argument { get; init; }

    /// <summary>Gets the literal the property is compared with, or null when it is compared with <see cref="Argument"/>.</summary>
    public SemanticValue? Literal { get; init; }
}

/// <summary>
/// Represents the assignment of a read-model property from a property of a matched node or edge.
/// </summary>
/// <param name="Property">The read-model property identity.</param>
/// <param name="Alias">The alias of the node or edge.</param>
/// <param name="Source">Its property.</param>
public sealed record SemanticGraphReturn(SemanticId Property, string Alias, string Source);

/// <summary>
/// Represents a query that reads a graph and returns rows as a read model.
/// </summary>
/// <param name="Name">The query name.</param>
/// <param name="Graph">The graph it reads.</param>
/// <param name="ReadModel">The read model the rows are returned as.</param>
/// <param name="Cardinality">How many rows it returns.</param>
/// <param name="Delivery">Whether it is answered once or re-evaluated as the graph changes.</param>
/// <param name="Arguments">The arguments it takes.</param>
/// <param name="Matches">The path patterns.</param>
/// <param name="Conditions">The conditions the rows satisfy.</param>
/// <param name="Returns">The read-model property assignments.</param>
public sealed record SemanticGraphQuery(
    string Name,
    string Graph,
    SemanticId ReadModel,
    SemanticQueryCardinality Cardinality,
    SemanticQueryDelivery Delivery,
    ImmutableArray<SemanticGraphQueryArgument> Arguments,
    ImmutableArray<SemanticGraphMatch> Matches,
    ImmutableArray<SemanticGraphCondition> Conditions,
    ImmutableArray<SemanticGraphReturn> Returns)
{
    /// <summary>Gets the effective authorization, or null for an unrestricted query.</summary>
    public SemanticAuthorization? Authorization { get; init; }
}

/// <summary>
/// Represents one expected row of a graph query.
/// </summary>
/// <param name="Values">The asserted read-model properties.</param>
public sealed record SemanticGraphRow(ImmutableArray<SemanticGraphPropertyValue> Values);

/// <summary>
/// Represents a graph query a specification runs, with the rows it expects.
/// </summary>
/// <param name="Query">The graph query name.</param>
/// <param name="Exactly">Whether every property of a row must match rather than the asserted subset.</param>
/// <param name="Arguments">The argument values.</param>
/// <param name="Rows">The expected rows, compared without regard to order. The row count is exact.</param>
public sealed record SemanticGraphQueryExpectation(string Query, bool Exactly, ImmutableArray<SemanticGraphPropertyValue> Arguments, ImmutableArray<SemanticGraphRow> Rows);

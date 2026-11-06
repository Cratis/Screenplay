// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a query that reads a graph and returns an ordinary read model, declared
/// <c language="screenplay">query &lt;Name&gt; =&gt; [observable] &lt;ReadModel&gt;[] graph &lt;Graph&gt;</c>.
/// </summary>
/// <param name="Name">The query name.</param>
/// <param name="ReturnType">The read model the rows are returned as.</param>
/// <param name="Graph">The graph the query reads.</param>
/// <param name="By">The single key argument, when declared.</param>
/// <param name="Filters">The filter arguments.</param>
/// <param name="Authorize">The authorization requirement, when declared.</param>
/// <param name="Matches">The path patterns, in declaration order.</param>
/// <param name="Conditions">The equality conditions the rows must satisfy.</param>
/// <param name="Returns">The assignments of read-model properties.</param>
/// <param name="Location">The source location.</param>
public record GraphQuerySyntax(
    string Name,
    TypeRefSyntax ReturnType,
    string Graph,
    QueryParameterSyntax? By,
    IEnumerable<QueryParameterSyntax> Filters,
    AuthorizeSyntax? Authorize,
    IEnumerable<GraphMatchSyntax> Matches,
    IEnumerable<GraphConditionSyntax> Conditions,
    IEnumerable<GraphReturnSyntax> Returns,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>Gets whether the query is re-evaluated when a projection of its graph writes.</summary>
    public bool IsObservable { get; init; }

    /// <summary>Gets the description, when declared.</summary>
    public string? Description { get; init; }
}

/// <summary>
/// Represents one <c>match</c> or <c>optional match</c> path pattern: a start node followed by hops.
/// </summary>
/// <param name="IsOptional">Whether the path may be absent from a row.</param>
/// <param name="Start">The first node of the path.</param>
/// <param name="Hops">The hops that follow it.</param>
/// <param name="Location">The source location.</param>
public record GraphMatchSyntax(bool IsOptional, GraphPatternNodeSyntax Start, IEnumerable<GraphPatternHopSyntax> Hops, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a node in a path pattern, <c>alias:Label</c> or <c>Label</c>.
/// </summary>
/// <param name="Alias">The alias, or null when the pattern does not name it.</param>
/// <param name="Label">The node label.</param>
/// <param name="Location">The source location.</param>
public record GraphPatternNodeSyntax(string? Alias, string Label, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an edge and the node it leads to in a path pattern.
/// </summary>
/// <param name="Alias">The edge alias, or null.</param>
/// <param name="Type">The edge type.</param>
/// <param name="Outgoing">Whether the edge points away from the previous node (<c>-T-&gt;</c>) rather than towards it (<c>&lt;-T-</c>).</param>
/// <param name="Node">The node the hop leads to.</param>
/// <param name="Location">The source location.</param>
public record GraphPatternHopSyntax(string? Alias, string Type, bool Outgoing, GraphPatternNodeSyntax Node, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an equality condition between a property of a matched node or edge and an argument or a literal.
/// </summary>
/// <param name="Alias">The alias of the node or edge.</param>
/// <param name="Property">The property of it.</param>
/// <param name="Value">The argument (<c>$name</c>) or the literal.</param>
/// <param name="Location">The source location.</param>
public record GraphConditionSyntax(string Alias, string Property, ExpressionSyntax Value, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the assignment of a read-model property from a property of a matched node or edge.
/// </summary>
/// <param name="Property">The read-model property.</param>
/// <param name="Alias">The alias of the node or edge.</param>
/// <param name="Source">The property of it.</param>
/// <param name="Location">The source location.</param>
public record GraphReturnSyntax(string Property, string Alias, string Source, SourceLocation Location) : SyntaxNode(Location);

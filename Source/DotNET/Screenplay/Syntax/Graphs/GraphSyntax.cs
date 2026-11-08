// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// A <c>graph</c> declaration - the shape of a property graph: node labels and edge types with typed properties.
/// </summary>
/// <param name="Name">The graph name.</param>
/// <param name="Nodes">The <see cref="GraphNodeSyntax">node declarations</see>.</param>
/// <param name="Edges">The <see cref="GraphEdgeSyntax">edge declarations</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the declaration.</param>
/// <param name="Description">An optional description.</param>
public record GraphSyntax(
    string Name,
    IEnumerable<GraphNodeSyntax> Nodes,
    IEnumerable<GraphEdgeSyntax> Edges,
    SourceLocation Location,
    string? Description = null) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional file the graph is generated into.
    /// </summary>
    public FileReferenceSyntax? File { get; init; }

    /// <summary>
    /// Gets the optional Markdown documentation of the graph.
    /// </summary>
    public string? Documentation { get; init; }

    /// <summary>
    /// Gets the name of the sink this graph is stored in, or <c>null</c> when it uses the implicit default sink for graphs.
    /// Written as a bare <c>sink &lt;Name&gt;</c> body directive; realization metadata, not part of the executable semantic model.
    /// </summary>
    public string? Sink { get; init; }
}

/// <summary>
/// A <c>node</c> declaration inside a <see cref="GraphSyntax"/> - a label with typed properties, exactly one of them the <c>key</c>.
/// </summary>
/// <param name="Label">The node label.</param>
/// <param name="Properties">The node's <see cref="PropertySyntax">properties</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the declaration.</param>
public record GraphNodeSyntax(
    string Label,
    IEnumerable<PropertySyntax> Properties,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// An <c>edge</c> declaration inside a <see cref="GraphSyntax"/> - a typed, directed relationship between two node labels.
/// </summary>
/// <param name="Type">The edge type.</param>
/// <param name="Source">The label of the source node.</param>
/// <param name="Target">The label of the target node.</param>
/// <param name="Properties">The edge's <see cref="PropertySyntax">properties</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the declaration.</param>
public record GraphEdgeSyntax(
    string Type,
    string Source,
    string Target,
    IEnumerable<PropertySyntax> Properties,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the name of the edge property that makes two edges between the same pair distinct, when the edge allows several.
    /// </summary>
    public string? IdentifiedBy { get; init; }
}

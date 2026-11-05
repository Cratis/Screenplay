// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// A <c>projection &lt;Name&gt; =&gt; graph &lt;Graph&gt;</c> declaration - events projected onto a property graph.
/// </summary>
/// <param name="Name">The projection name.</param>
/// <param name="Graph">The name of the <see cref="GraphSyntax"/> the projection builds.</param>
/// <param name="Blocks">The <see cref="GraphNodeBlockSyntax">node</see> and <see cref="GraphEdgeBlockSyntax">edge</see> blocks.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the declaration.</param>
public record GraphProjectionSyntax(
    string Name,
    string Graph,
    IEnumerable<ProjectionBlockSyntax> Blocks,
    SourceLocation Location) : SyntaxNode(Location), ISourceSyntax
{
    /// <summary>
    /// Gets the source options in force where the projection was declared.
    /// </summary>
    public SourceOptions SourceOptions { get; init; } = SourceOptions.Legacy;

    /// <summary>
    /// Gets the optional file the projection is generated into.
    /// </summary>
    public FileReferenceSyntax? File { get; init; }

    /// <summary>
    /// Gets the optional description of the projection.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// A <c>node &lt;Label&gt;</c> block of a graph projection - how events create, change and remove one kind of node.
/// </summary>
/// <param name="Label">The node label.</param>
/// <param name="Blocks">The handlers: <see cref="GraphFromSyntax"/>, <see cref="EverySyntax"/> and <see cref="GraphRemoveSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the block.</param>
public record GraphNodeBlockSyntax(
    string Label,
    IEnumerable<ProjectionBlockSyntax> Blocks,
    SourceLocation Location) : ProjectionBlockSyntax(Location);

/// <summary>
/// An <c>edge &lt;TYPE&gt;</c> block of a graph projection - how events create, change and remove one kind of edge.
/// </summary>
/// <param name="Type">The edge type.</param>
/// <param name="Blocks">The handlers: <see cref="GraphFromSyntax"/>, <see cref="EverySyntax"/> and <see cref="GraphRemoveSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the block.</param>
public record GraphEdgeBlockSyntax(
    string Type,
    IEnumerable<ProjectionBlockSyntax> Blocks,
    SourceLocation Location) : ProjectionBlockSyntax(Location);

/// <summary>
/// A <c>from &lt;Event&gt;</c> handler in a graph node or edge block, with the expressions that identify the element and its property mappings.
/// </summary>
/// <param name="Event">The event type.</param>
/// <param name="Key">The expression of the node key; set in a node block.</param>
/// <param name="Source">The expression of the source node key; set in an edge block.</param>
/// <param name="Target">The expression of the target node key; set in an edge block.</param>
/// <param name="Mappings">The property <see cref="MappingSyntax">mappings</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the handler.</param>
public record GraphFromSyntax(
    string Event,
    ExpressionSyntax? Key,
    ExpressionSyntax? Source,
    ExpressionSyntax? Target,
    IEnumerable<MappingSyntax> Mappings,
    SourceLocation Location) : ProjectionBlockSyntax(Location)
{
    /// <summary>
    /// Gets the expression of the identifying edge property, when the edge declares <c>identified by</c>.
    /// </summary>
    public ExpressionSyntax? Identity { get; init; }
}

/// <summary>
/// A <c>remove with &lt;Event&gt;</c> handler in a graph node or edge block.
/// </summary>
/// <param name="Event">The event type.</param>
/// <param name="Key">The expression of the node key; set in a node block.</param>
/// <param name="Source">The expression of the source node key; set in an edge block.</param>
/// <param name="Target">The expression of the target node key; set in an edge block.</param>
/// <param name="Detach">Whether removing a node also removes the edges attached to it.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the handler.</param>
public record GraphRemoveSyntax(
    string Event,
    ExpressionSyntax? Key,
    ExpressionSyntax? Source,
    ExpressionSyntax? Target,
    bool Detach,
    SourceLocation Location) : ProjectionBlockSyntax(Location)
{
    /// <summary>
    /// Gets the expression of the identifying edge property, when the edge declares <c>identified by</c>.
    /// </summary>
    public ExpressionSyntax? Identity { get; init; }
}

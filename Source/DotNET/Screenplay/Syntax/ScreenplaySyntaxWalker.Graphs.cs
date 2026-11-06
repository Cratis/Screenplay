// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of graph declarations, graph projections and graph specification steps.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="GraphSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="GraphSyntax"/> to visit.</param>
    /// <summary>
    /// Visits a <see cref="GraphQuerySyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="GraphQuerySyntax"/> to visit.</param>
    public virtual void VisitGraphQuery(GraphQuerySyntax syntax)
    {
        VisitNode(syntax);
        VisitTypeRef(syntax.ReturnType);
        if (syntax.By is not null) VisitQueryParameter(syntax.By);
        foreach (var filter in syntax.Filters) VisitQueryParameter(filter);
        if (syntax.Authorize is not null) VisitAuthorize(syntax.Authorize);
        foreach (var match in syntax.Matches)
        {
            VisitNode(match);
            VisitNode(match.Start);
            foreach (var hop in match.Hops)
            {
                VisitNode(hop);
                VisitNode(hop.Node);
            }
        }

        foreach (var condition in syntax.Conditions)
        {
            VisitNode(condition);
            VisitExpression(condition.Value);
        }

        foreach (var @return in syntax.Returns) VisitNode(@return);
    }

    public virtual void VisitGraph(GraphSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.File is not null) VisitFileReference(syntax.File);
        foreach (var node in syntax.Nodes) VisitGraphNode(node);
        foreach (var edge in syntax.Edges) VisitGraphEdge(edge);
    }

    /// <summary>
    /// Visits a <see cref="GraphNodeSyntax"/> node and its properties.
    /// </summary>
    /// <param name="syntax">The <see cref="GraphNodeSyntax"/> to visit.</param>
    public virtual void VisitGraphNode(GraphNodeSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var property in syntax.Properties) VisitProperty(property);
    }

    /// <summary>
    /// Visits a <see cref="GraphEdgeSyntax"/> node and its properties.
    /// </summary>
    /// <param name="syntax">The <see cref="GraphEdgeSyntax"/> to visit.</param>
    public virtual void VisitGraphEdge(GraphEdgeSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var property in syntax.Properties) VisitProperty(property);
    }

    /// <summary>
    /// Visits a <see cref="GraphProjectionSyntax"/> node and its blocks.
    /// </summary>
    /// <param name="syntax">The <see cref="GraphProjectionSyntax"/> to visit.</param>
    public virtual void VisitGraphProjection(GraphProjectionSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.File is not null) VisitFileReference(syntax.File);
        foreach (var block in syntax.Blocks) VisitProjectionBlock(block);
    }

    /// <summary>
    /// Visits a <see cref="GraphNodeBlockSyntax"/> node and its handlers.
    /// </summary>
    /// <param name="syntax">The <see cref="GraphNodeBlockSyntax"/> to visit.</param>
    public virtual void VisitGraphNodeBlock(GraphNodeBlockSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var block in syntax.Blocks) VisitProjectionBlock(block);
    }

    /// <summary>
    /// Visits a <see cref="GraphEdgeBlockSyntax"/> node and its handlers.
    /// </summary>
    /// <param name="syntax">The <see cref="GraphEdgeBlockSyntax"/> to visit.</param>
    public virtual void VisitGraphEdgeBlock(GraphEdgeBlockSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var block in syntax.Blocks) VisitProjectionBlock(block);
    }

    /// <summary>
    /// Visits a <see cref="GraphFromSyntax"/> node and its expressions and mappings.
    /// </summary>
    /// <param name="syntax">The <see cref="GraphFromSyntax"/> to visit.</param>
    public virtual void VisitGraphFrom(GraphFromSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.Key is not null) VisitExpression(syntax.Key);
        if (syntax.Source is not null) VisitExpression(syntax.Source);
        if (syntax.Target is not null) VisitExpression(syntax.Target);
        if (syntax.Identity is not null) VisitExpression(syntax.Identity);
        foreach (var mapping in syntax.Mappings) VisitMapping(mapping);
    }

    /// <summary>
    /// Visits a <see cref="GraphRemoveSyntax"/> node and its expressions.
    /// </summary>
    /// <param name="syntax">The <see cref="GraphRemoveSyntax"/> to visit.</param>
    public virtual void VisitGraphRemove(GraphRemoveSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.Key is not null) VisitExpression(syntax.Key);
        if (syntax.Source is not null) VisitExpression(syntax.Source);
        if (syntax.Target is not null) VisitExpression(syntax.Target);
        if (syntax.Identity is not null) VisitExpression(syntax.Identity);
    }

    /// <summary>
    /// Visits a <see cref="SpecificationGraphSyntax"/> node and its element steps.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationGraphSyntax"/> to visit.</param>
    public virtual void VisitSpecificationGraph(SpecificationGraphSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var element in syntax.Elements) VisitSpecificationGraphElement(element);
    }

    /// <summary>
    /// Visits a <see cref="SpecificationGraphElementSyntax"/> node and its expressions and values.
    /// </summary>
    /// <param name="syntax">The <see cref="SpecificationGraphElementSyntax"/> to visit.</param>
    public virtual void VisitSpecificationGraphElement(SpecificationGraphElementSyntax syntax)
    {
        VisitNode(syntax);
        switch (syntax)
        {
            case SpecificationGraphNodeSyntax node:
                VisitExpression(node.Key);
                break;
            case SpecificationGraphEdgeSyntax edge:
                VisitExpression(edge.Source);
                VisitExpression(edge.Target);
                if (edge.Identity is not null) VisitExpression(edge.Identity);
                break;
        }

        foreach (var property in syntax.Properties) VisitPropertyMapping(property);
    }
}

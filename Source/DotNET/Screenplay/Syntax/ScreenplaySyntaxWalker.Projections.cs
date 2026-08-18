// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of projections and the blocks making up their bodies.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="ProjectionSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ProjectionSyntax"/> to visit.</param>
    /// <remarks>
    /// This is one of the four roots - a projection compiled on its own with
    /// <see cref="IScreenplayCompiler.CompileProjection(string)"/> starts here.
    /// </remarks>
    public virtual void VisitProjection(ProjectionSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.File is not null)
        {
            VisitFileReference(syntax.File);
        }

        if (syntax.Key is not null)
        {
            VisitKey(syntax.Key);
        }

        foreach (var block in syntax.Blocks)
        {
            VisitProjectionBlock(block);
        }
    }

    /// <summary>
    /// Visits a <see cref="ProjectionBlockSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="ProjectionBlockSyntax"/> to visit.</param>
    /// <remarks>
    /// A block kind this walker does not know is visited as a node and not descended into, so a kind added
    /// to the language later cannot break an existing walker.
    /// </remarks>
    public virtual void VisitProjectionBlock(ProjectionBlockSyntax syntax)
    {
        switch (syntax)
        {
            case FromSyntax from:
                VisitFrom(from);
                break;
            case EverySyntax every:
                VisitEvery(every);
                break;
            case AllSyntax all:
                VisitAll(all);
                break;
            case JoinSyntax join:
                VisitJoin(join);
                break;
            case ChildrenSyntax children:
                VisitChildren(children);
                break;
            case NestedSyntax nested:
                VisitNested(nested);
                break;
            case ClearWithSyntax clearWith:
                VisitClearWith(clearWith);
                break;
            case RemoveWithSyntax removeWith:
                VisitRemoveWith(removeWith);
                break;
            case RemoveViaJoinSyntax removeViaJoin:
                VisitRemoveViaJoin(removeViaJoin);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="FromSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="FromSyntax"/> to visit.</param>
    public virtual void VisitFrom(FromSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var @event in syntax.Events)
        {
            VisitEventSpec(@event);
        }

        if (syntax.Key is not null)
        {
            VisitKey(syntax.Key);
        }

        if (syntax.ParentKey is not null)
        {
            VisitExpression(syntax.ParentKey);
        }

        foreach (var mapping in syntax.Mappings)
        {
            VisitMapping(mapping);
        }
    }

    /// <summary>
    /// Visits an <see cref="EventSpecSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="EventSpecSyntax"/> to visit.</param>
    public virtual void VisitEventSpec(EventSpecSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Key is not null)
        {
            VisitExpression(syntax.Key);
        }
    }

    /// <summary>
    /// Visits an <see cref="EverySyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="EverySyntax"/> to visit.</param>
    public virtual void VisitEvery(EverySyntax syntax)
    {
        VisitNode(syntax);

        foreach (var mapping in syntax.Mappings)
        {
            VisitMapping(mapping);
        }
    }

    /// <summary>
    /// Visits an <see cref="AllSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="AllSyntax"/> to visit.</param>
    public virtual void VisitAll(AllSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var mapping in syntax.Mappings)
        {
            VisitMapping(mapping);
        }
    }

    /// <summary>
    /// Visits a <see cref="JoinSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="JoinSyntax"/> to visit.</param>
    public virtual void VisitJoin(JoinSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var @event in syntax.Events)
        {
            VisitJoinEvent(@event);
        }
    }

    /// <summary>
    /// Visits a <see cref="JoinEventSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="JoinEventSyntax"/> to visit.</param>
    public virtual void VisitJoinEvent(JoinEventSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var mapping in syntax.Mappings)
        {
            VisitMapping(mapping);
        }
    }

    /// <summary>
    /// Visits a <see cref="ChildrenSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ChildrenSyntax"/> to visit.</param>
    public virtual void VisitChildren(ChildrenSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.IdentifiedBy);

        foreach (var block in syntax.Blocks)
        {
            VisitProjectionBlock(block);
        }
    }

    /// <summary>
    /// Visits a <see cref="NestedSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="NestedSyntax"/> to visit.</param>
    public virtual void VisitNested(NestedSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var block in syntax.Blocks)
        {
            VisitProjectionBlock(block);
        }
    }

    /// <summary>
    /// Visits a <see cref="ClearWithSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ClearWithSyntax"/> to visit.</param>
    public virtual void VisitClearWith(ClearWithSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="RemoveWithSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="RemoveWithSyntax"/> to visit.</param>
    public virtual void VisitRemoveWith(RemoveWithSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Key is not null)
        {
            VisitExpression(syntax.Key);
        }

        if (syntax.ParentKey is not null)
        {
            VisitExpression(syntax.ParentKey);
        }
    }

    /// <summary>
    /// Visits a <see cref="RemoveViaJoinSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="RemoveViaJoinSyntax"/> to visit.</param>
    public virtual void VisitRemoveViaJoin(RemoveViaJoinSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Key is not null)
        {
            VisitExpression(syntax.Key);
        }
    }
}

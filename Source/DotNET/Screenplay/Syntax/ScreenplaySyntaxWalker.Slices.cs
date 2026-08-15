// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of the remaining members of a slice - queries, reactions and constraints.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="QuerySyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="QuerySyntax"/> to visit.</param>
    public virtual void VisitQuery(QuerySyntax syntax)
    {
        VisitNode(syntax);
        VisitTypeRef(syntax.ReturnType);

        if (syntax.By is not null)
        {
            VisitQueryParameter(syntax.By);
        }

        foreach (var filter in syntax.Filters)
        {
            VisitQueryParameter(filter);
        }

        if (syntax.Authorize is not null)
        {
            VisitAuthorize(syntax.Authorize);
        }

        if (syntax.Performer is not null)
        {
            VisitPerformer(syntax.Performer);
        }
    }

    /// <summary>
    /// Visits a <see cref="QueryParameterSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="QueryParameterSyntax"/> to visit.</param>
    public virtual void VisitQueryParameter(QueryParameterSyntax syntax)
    {
        VisitNode(syntax);
        VisitTypeRef(syntax.Type);

        if (syntax.Source is not null)
        {
            VisitExpression(syntax.Source);
        }
    }

    /// <summary>
    /// Visits a <see cref="PerformerSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="PerformerSyntax"/> to visit.</param>
    public virtual void VisitPerformer(PerformerSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.File is not null)
        {
            VisitFileReference(syntax.File);
        }

        if (syntax.Code is not null)
        {
            VisitCodeBlock(syntax.Code);
        }
    }

    /// <summary>
    /// Visits a <see cref="ReactionSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ReactionSyntax"/> to visit.</param>
    public virtual void VisitReaction(ReactionSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var trigger in syntax.Triggers)
        {
            VisitReactionTrigger(trigger);
        }

        if (syntax.Where is not null)
        {
            VisitCondition(syntax.Where);
        }
    }

    /// <summary>
    /// Visits a <see cref="ReactionTriggerSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ReactionTriggerSyntax"/> to visit.</param>
    public virtual void VisitReactionTrigger(ReactionTriggerSyntax syntax)
    {
        VisitNode(syntax);
        VisitNode(syntax.Source);

        foreach (var datum in syntax.Data)
        {
            VisitTriggerData(datum);
        }

        foreach (var produces in syntax.Produces ?? [])
        {
            VisitProduces(produces);
        }

        foreach (var invokes in syntax.Invokes ?? [])
        {
            VisitInvokes(invokes);
        }

        if (syntax.File is not null)
        {
            VisitFileReference(syntax.File);
        }

        if (syntax.Code is not null)
        {
            VisitCodeBlock(syntax.Code);
        }
    }

    /// <summary>
    /// Visits a <see cref="TriggerDataSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="TriggerDataSyntax"/> to visit.</param>
    public virtual void VisitTriggerData(TriggerDataSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Type is not null)
        {
            VisitTypeRef(syntax.Type);
        }
    }

    /// <summary>
    /// Visits a <see cref="ConstraintSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="ConstraintSyntax"/> to visit.</param>
    /// <remarks>
    /// A constraint kind this walker does not know is visited as a node and not descended into, so a kind
    /// added to the language later cannot break an existing walker.
    /// </remarks>
    public virtual void VisitConstraint(ConstraintSyntax syntax)
    {
        switch (syntax)
        {
            case UniquePropertyConstraintSyntax uniqueProperty:
                VisitUniquePropertyConstraint(uniqueProperty);
                break;
            case UniqueEventConstraintSyntax uniqueEvent:
                VisitUniqueEventConstraint(uniqueEvent);
                break;
            case FileConstraintSyntax file:
                VisitFileConstraint(file);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="UniquePropertyConstraintSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="UniquePropertyConstraintSyntax"/> to visit.</param>
    public virtual void VisitUniquePropertyConstraint(UniquePropertyConstraintSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="UniqueEventConstraintSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="UniqueEventConstraintSyntax"/> to visit.</param>
    public virtual void VisitUniqueEventConstraint(UniqueEventConstraintSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="FileConstraintSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="FileConstraintSyntax"/> to visit.</param>
    public virtual void VisitFileConstraint(FileConstraintSyntax syntax)
    {
        VisitNode(syntax);
        VisitFileReference(syntax.File);
    }
}

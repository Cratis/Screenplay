// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of the remaining members of a slice - queries, reactors and constraints.
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
    /// Visits a <see cref="ReactorSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ReactorSyntax"/> to visit.</param>
    public virtual void VisitReactor(ReactorSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var trigger in syntax.Triggers)
        {
            VisitReactorTrigger(trigger);
        }
    }

    /// <summary>
    /// Visits a <see cref="ReactorTriggerSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ReactorTriggerSyntax"/> to visit.</param>
    public virtual void VisitReactorTrigger(ReactorTriggerSyntax syntax)
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

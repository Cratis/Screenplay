// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of the keys and mapping lines a projection body is built from.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="KeySyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="KeySyntax"/> to visit.</param>
    /// <remarks>
    /// A key kind this walker does not know is visited as a node and not descended into, so a kind added
    /// to the language later cannot break an existing walker.
    /// </remarks>
    public virtual void VisitKey(KeySyntax syntax)
    {
        switch (syntax)
        {
            case ExpressionKeySyntax expression:
                VisitExpressionKey(expression);
                break;
            case CompositeKeySyntax composite:
                VisitCompositeKey(composite);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits an <see cref="ExpressionKeySyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ExpressionKeySyntax"/> to visit.</param>
    public virtual void VisitExpressionKey(ExpressionKeySyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Expression);
    }

    /// <summary>
    /// Visits a <see cref="CompositeKeySyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="CompositeKeySyntax"/> to visit.</param>
    public virtual void VisitCompositeKey(CompositeKeySyntax syntax)
    {
        VisitNode(syntax);

        foreach (var part in syntax.Parts)
        {
            VisitKeyPart(part);
        }
    }

    /// <summary>
    /// Visits a <see cref="KeyPartSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="KeyPartSyntax"/> to visit.</param>
    public virtual void VisitKeyPart(KeyPartSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Expression);
    }

    /// <summary>
    /// Visits a <see cref="MappingSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="MappingSyntax"/> to visit.</param>
    /// <remarks>
    /// A mapping kind this walker does not know is visited as a node and not descended into, so a kind
    /// added to the language later cannot break an existing walker.
    /// </remarks>
    public virtual void VisitMapping(MappingSyntax syntax)
    {
        switch (syntax)
        {
            case SetMappingSyntax set:
                VisitSetMapping(set);
                break;
            case IncrementMappingSyntax increment:
                VisitIncrementMapping(increment);
                break;
            case DecrementMappingSyntax decrement:
                VisitDecrementMapping(decrement);
                break;
            case CountMappingSyntax count:
                VisitCountMapping(count);
                break;
            case AddMappingSyntax add:
                VisitAddMapping(add);
                break;
            case SubtractMappingSyntax subtract:
                VisitSubtractMapping(subtract);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="SetMappingSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SetMappingSyntax"/> to visit.</param>
    public virtual void VisitSetMapping(SetMappingSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Source);
    }

    /// <summary>
    /// Visits an <see cref="IncrementMappingSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="IncrementMappingSyntax"/> to visit.</param>
    public virtual void VisitIncrementMapping(IncrementMappingSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="DecrementMappingSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="DecrementMappingSyntax"/> to visit.</param>
    public virtual void VisitDecrementMapping(DecrementMappingSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="CountMappingSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="CountMappingSyntax"/> to visit.</param>
    public virtual void VisitCountMapping(CountMappingSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="AddMappingSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="AddMappingSyntax"/> to visit.</param>
    public virtual void VisitAddMapping(AddMappingSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Value);
    }

    /// <summary>
    /// Visits a <see cref="SubtractMappingSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="SubtractMappingSyntax"/> to visit.</param>
    public virtual void VisitSubtractMapping(SubtractMappingSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Value);
    }
}

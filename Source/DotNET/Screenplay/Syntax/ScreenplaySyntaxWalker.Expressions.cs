// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of expressions - the values mappings, conditions, keys and settings are built from.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits an <see cref="ExpressionSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="ExpressionSyntax"/> to visit.</param>
    /// <remarks>
    /// An expression kind this walker does not know - one a sub-language contributes, or one added to the
    /// language later - is visited as a node and not descended into, so it cannot break an existing walker.
    /// </remarks>
    public virtual void VisitExpression(ExpressionSyntax syntax)
    {
        switch (syntax)
        {
            case LiteralExpressionSyntax literal:
                VisitLiteralExpression(literal);
                break;
            case PathExpressionSyntax path:
                VisitPathExpression(path);
                break;
            case ContextExpressionSyntax context:
                VisitContextExpression(context);
                break;
            case EnvironmentExpressionSyntax environment:
                VisitEnvironmentExpression(environment);
                break;
            case SecretExpressionSyntax secret:
                VisitSecretExpression(secret);
                break;
            case StringsExpressionSyntax strings:
                VisitStringsExpression(strings);
                break;
            case RawExpressionSyntax raw:
                VisitRawExpression(raw);
                break;
            case SourceItemExpressionSyntax sourceItem:
                VisitSourceItemExpression(sourceItem);
                break;
            case EventSourceIdExpressionSyntax eventSourceId:
                VisitEventSourceIdExpression(eventSourceId);
                break;
            case EventContextExpressionSyntax eventContext:
                VisitEventContextExpression(eventContext);
                break;
            case CausedByExpressionSyntax causedBy:
                VisitCausedByExpression(causedBy);
                break;
            case TemplateExpressionSyntax template:
                VisitTemplateExpression(template);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="LiteralExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="LiteralExpressionSyntax"/> to visit.</param>
    public virtual void VisitLiteralExpression(LiteralExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="PathExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="PathExpressionSyntax"/> to visit.</param>
    public virtual void VisitPathExpression(PathExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="ContextExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ContextExpressionSyntax"/> to visit.</param>
    public virtual void VisitContextExpression(ContextExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="EnvironmentExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="EnvironmentExpressionSyntax"/> to visit.</param>
    public virtual void VisitEnvironmentExpression(EnvironmentExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="SecretExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SecretExpressionSyntax"/> to visit.</param>
    public virtual void VisitSecretExpression(SecretExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="StringsExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="StringsExpressionSyntax"/> to visit.</param>
    public virtual void VisitStringsExpression(StringsExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="RawExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="RawExpressionSyntax"/> to visit.</param>
    public virtual void VisitRawExpression(RawExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="SourceItemExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="SourceItemExpressionSyntax"/> to visit.</param>
    public virtual void VisitSourceItemExpression(SourceItemExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="EventSourceIdExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="EventSourceIdExpressionSyntax"/> to visit.</param>
    public virtual void VisitEventSourceIdExpression(EventSourceIdExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits an <see cref="EventContextExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="EventContextExpressionSyntax"/> to visit.</param>
    public virtual void VisitEventContextExpression(EventContextExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="CausedByExpressionSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="CausedByExpressionSyntax"/> to visit.</param>
    public virtual void VisitCausedByExpression(CausedByExpressionSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="TemplateExpressionSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="TemplateExpressionSyntax"/> to visit.</param>
    public virtual void VisitTemplateExpression(TemplateExpressionSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var part in syntax.Parts)
        {
            VisitTemplatePart(part);
        }
    }

    /// <summary>
    /// Visits a <see cref="TemplatePartSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="TemplatePartSyntax"/> to visit.</param>
    /// <remarks>
    /// A part kind this walker does not know is visited as a node and not descended into, so a kind added
    /// to the language later cannot break an existing walker.
    /// </remarks>
    public virtual void VisitTemplatePart(TemplatePartSyntax syntax)
    {
        switch (syntax)
        {
            case TemplateTextSyntax text:
                VisitTemplateText(text);
                break;
            case TemplateInterpolationSyntax interpolation:
                VisitTemplateInterpolation(interpolation);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="TemplateTextSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="TemplateTextSyntax"/> to visit.</param>
    public virtual void VisitTemplateText(TemplateTextSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="TemplateInterpolationSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="TemplateInterpolationSyntax"/> to visit.</param>
    public virtual void VisitTemplateInterpolation(TemplateInterpolationSyntax syntax)
    {
        VisitNode(syntax);
        VisitExpression(syntax.Expression);
    }
}

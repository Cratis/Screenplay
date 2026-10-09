// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Traversal of screens and the directives making up their bodies.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="ScreenSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenSyntax"/> to visit.</param>
    public virtual void VisitScreen(ScreenSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.File is not null)
        {
            VisitFileReference(syntax.File);
        }

        foreach (var directive in syntax.Directives)
        {
            VisitScreenDirective(directive);
        }
    }

    /// <summary>
    /// Visits a <see cref="ScreenDirectiveSyntax"/> node by dispatching to the method for its kind.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenDirectiveSyntax"/> to visit.</param>
    /// <remarks>
    /// A directive kind this walker does not know is visited as a node and not descended into, so a kind
    /// added to the language later cannot break an existing walker.
    /// </remarks>
    public virtual void VisitScreenDirective(ScreenDirectiveSyntax syntax)
    {
        switch (syntax)
        {
            case ScreenDataSyntax data:
                VisitScreenData(data);
                break;
            case ScreenActionSyntax action:
                VisitScreenAction(action);
                break;
            case ScreenGuardedActionSyntax guarded:
                VisitScreenGuardedAction(guarded);
                break;
            case ScreenNavigateSyntax navigate:
                VisitScreenNavigate(navigate);
                break;
            case ScreenTemplateReferenceSyntax template:
                VisitScreenTemplateReference(template);
                break;
            case ScreenSlotSyntax slot:
                VisitScreenSlot(slot);
                break;
            case ScreenSectionSyntax section:
                VisitScreenSection(section);
                break;
            case ScreenTitleSyntax title:
                VisitScreenTitle(title);
                break;
            case ScreenTableSyntax table:
                VisitScreenTable(table);
                break;
            case ScreenSummarySyntax summary:
                VisitScreenSummary(summary);
                break;
            case ScreenComponentSyntax component:
                VisitScreenComponent(component);
                break;
            case ScreenToolbarSyntax toolbar:
                VisitScreenToolbar(toolbar);
                break;
            case ScreenCodeSyntax code:
                VisitScreenCode(code);
                break;
            case ScreenBehaviorSyntax behavior:
                VisitScreenBehavior(behavior);
                break;
            case ScreenUsesBehaviorSyntax uses:
                VisitScreenUsesBehavior(uses);
                break;
            default:
                VisitNode(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits a <see cref="ScreenDataSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenDataSyntax"/> to visit.</param>
    public virtual void VisitScreenData(ScreenDataSyntax syntax)
    {
        VisitNode(syntax);
        VisitTypeRef(syntax.Type);
    }

    /// <summary>
    /// Visits a <see cref="ScreenActionSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenActionSyntax"/> to visit.</param>
    public virtual void VisitScreenAction(ScreenActionSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Navigate is not null)
        {
            VisitScreenNavigate(syntax.Navigate);
        }
    }

    /// <summary>
    /// Visits a <see cref="ScreenNavigateSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenNavigateSyntax"/> to visit.</param>
    public virtual void VisitScreenNavigate(ScreenNavigateSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var parameter in syntax.Parameters) VisitNode(parameter);
        foreach (var parameter in syntax.Parameters) VisitNode(parameter.Binding);
    }

    /// <summary>
    /// Visits a <see cref="ScreenTemplateReferenceSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenTemplateReferenceSyntax"/> to visit.</param>
    public virtual void VisitScreenTemplateReference(ScreenTemplateReferenceSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var slot in syntax.Slots)
        {
            VisitScreenSlot(slot);
        }
    }

    /// <summary>
    /// Visits a <see cref="ScreenSlotSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenSlotSyntax"/> to visit.</param>
    public virtual void VisitScreenSlot(ScreenSlotSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var directive in syntax.Directives)
        {
            VisitScreenDirective(directive);
        }
    }

    /// <summary>
    /// Visits a <see cref="ScreenSectionSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenSectionSyntax"/> to visit.</param>
    public virtual void VisitScreenSection(ScreenSectionSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var directive in syntax.Directives)
        {
            VisitScreenDirective(directive);
        }
    }

    /// <summary>
    /// Visits a <see cref="ScreenTitleSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenTitleSyntax"/> to visit.</param>
    public virtual void VisitScreenTitle(ScreenTitleSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="ScreenTableSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenTableSyntax"/> to visit.</param>
    public virtual void VisitScreenTable(ScreenTableSyntax syntax)
    {
        VisitNode(syntax);

        foreach (var column in syntax.Columns)
        {
            VisitScreenColumn(column);
        }

        if (syntax.RowClick is not null)
        {
            VisitScreenNavigate(syntax.RowClick);
        }

        foreach (var behavior in syntax.Behaviors)
        {
            VisitBehavior(behavior);
        }

        foreach (var uses in syntax.UsedBehaviors)
        {
            VisitUsesBehavior(uses);
        }
    }

    /// <summary>
    /// Visits a <see cref="ScreenColumnSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenColumnSyntax"/> to visit.</param>
    public virtual void VisitScreenColumn(ScreenColumnSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="ScreenSummarySyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenSummarySyntax"/> to visit.</param>
    public virtual void VisitScreenSummary(ScreenSummarySyntax syntax)
    {
        VisitNode(syntax);

        foreach (var field in syntax.Fields)
        {
            VisitScreenField(field);
        }
    }

    /// <summary>
    /// Visits a <see cref="ScreenFieldSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenFieldSyntax"/> to visit.</param>
    public virtual void VisitScreenField(ScreenFieldSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="ScreenComponentSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenComponentSyntax"/> to visit.</param>
    public virtual void VisitScreenComponent(ScreenComponentSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.Context is not null) VisitUiBinding(syntax.Context);
        foreach (var property in syntax.Properties)
        {
            VisitNode(property);
            if (property.Binding is not null) VisitUiBinding(property.Binding);
            if (property.Value is not null) VisitExpression(property.Value);
        }
        foreach (var exposed in syntax.Exposes)
        {
            VisitNode(exposed);
            VisitUiBinding(exposed.Binding);
        }
        foreach (var presentation in syntax.Presentation) VisitNode(presentation);
        foreach (var outlet in syntax.Outlets)
        {
            VisitNode(outlet);
            foreach (var directive in outlet.Directives) VisitScreenDirective(directive);
        }
        foreach (var behavior in syntax.Behaviors) VisitBehavior(behavior);
        foreach (var uses in syntax.UsedBehaviors) VisitUsesBehavior(uses);
    }

    /// <summary>
    /// Visits a <see cref="UiBindingSyntax"/> node and its literal value.
    /// </summary>
    /// <param name="syntax">The <see cref="UiBindingSyntax"/> to visit.</param>
    public virtual void VisitUiBinding(UiBindingSyntax syntax)
    {
        VisitNode(syntax);
        if (syntax.Literal is not null) VisitExpression(syntax.Literal);
    }

    /// <summary>
    /// Visits a <see cref="ScreenToolbarSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenToolbarSyntax"/> to visit.</param>
    public virtual void VisitScreenToolbar(ScreenToolbarSyntax syntax)
    {
        VisitNode(syntax);
        foreach (var item in syntax.Items)
        {
            VisitNode(item);
            foreach (var parameter in item.Parameters)
            {
                VisitNode(parameter);
                VisitUiBinding(parameter.Binding);
            }
            foreach (var presentation in item.Presentation) VisitNode(presentation);
        }
    }

    /// <summary>
    /// Visits a <see cref="ScreenCodeSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenCodeSyntax"/> to visit.</param>
    public virtual void VisitScreenCode(ScreenCodeSyntax syntax)
    {
        VisitNode(syntax);
        VisitCodeBlock(syntax.Code);
    }

    /// <summary>
    /// Visits a <see cref="ScreenBehaviorSyntax"/> node and the behavior it attaches.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenBehaviorSyntax"/> to visit.</param>
    public virtual void VisitScreenBehavior(ScreenBehaviorSyntax syntax)
    {
        VisitNode(syntax);
        VisitBehavior(syntax.Behavior);
    }

    /// <summary>
    /// Visits a <see cref="ScreenUsesBehaviorSyntax"/> node and the attachment it carries.
    /// </summary>
    /// <param name="syntax">The <see cref="ScreenUsesBehaviorSyntax"/> to visit.</param>
    public virtual void VisitScreenUsesBehavior(ScreenUsesBehaviorSyntax syntax)
    {
        VisitNode(syntax);
        VisitUsesBehavior(syntax.Uses);
    }
}

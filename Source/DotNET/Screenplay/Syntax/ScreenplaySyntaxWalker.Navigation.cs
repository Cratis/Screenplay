// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Walks outlets - the item lists layouts and templates offer - and their items.
/// </summary>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits an <see cref="OutletSyntax"/> and its logo, items and hidden items.
    /// </summary>
    /// <param name="syntax">The outlet.</param>
    public virtual void VisitOutlet(OutletSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Logo is not null)
        {
            VisitNavigationLogo(syntax.Logo);
        }

        foreach (var item in syntax.Items)
        {
            VisitOutletItem(item);
        }

        foreach (var hide in syntax.Hides)
        {
            VisitNode(hide);
        }
    }

    /// <summary>
    /// Visits one outlet item, dispatching on its form.
    /// </summary>
    /// <param name="syntax">The item.</param>
    public virtual void VisitOutletItem(OutletItemSyntax syntax)
    {
        switch (syntax)
        {
            case ToolbarButtonSyntax button:
                VisitToolbarButton(button);
                break;
            case NavigationItemSyntax item:
                VisitNavigationItem(item);
                break;
            case OutletGroupSyntax group:
                VisitOutletGroup(group);
                break;
            default:
                VisitNode(syntax);
                VisitOutletPosition(syntax);
                break;
        }
    }

    /// <summary>
    /// Visits an <see cref="OutletGroupSyntax"/> and its buttons.
    /// </summary>
    /// <param name="syntax">The group.</param>
    public virtual void VisitOutletGroup(OutletGroupSyntax syntax)
    {
        VisitNode(syntax);
        VisitOutletPosition(syntax);
        foreach (var button in syntax.Buttons)
        {
            VisitToolbarButton(button);
        }
    }

    /// <summary>
    /// Visits a <see cref="NavigationLogoSyntax"/>.
    /// </summary>
    /// <param name="syntax">The logo.</param>
    public virtual void VisitNavigationLogo(NavigationLogoSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="NavigationItemSyntax"/> and its target and sub-items.
    /// </summary>
    /// <param name="syntax">The item.</param>
    public virtual void VisitNavigationItem(NavigationItemSyntax syntax)
    {
        VisitNode(syntax);
        VisitOutletPosition(syntax);

        if (syntax.Target is not null)
        {
            VisitNavigationTarget(syntax.Target);
        }

        foreach (var child in syntax.Children)
        {
            VisitNavigationItem(child);
        }
    }

    /// <summary>
    /// Visits a <see cref="NavigationTargetSyntax"/>.
    /// </summary>
    /// <param name="syntax">The target.</param>
    public virtual void VisitNavigationTarget(NavigationTargetSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits where an outlet item stands, when it says.
    /// </summary>
    /// <param name="syntax">The item.</param>
    protected void VisitOutletPosition(OutletItemSyntax syntax)
    {
        if (syntax.Position is not null)
        {
            VisitNode(syntax.Position);
        }
    }
}

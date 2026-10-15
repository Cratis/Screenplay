// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <content>
/// Visiting of navigation regions, their logo and items.
/// </content>
public abstract partial class ScreenplaySyntaxWalker
{
    /// <summary>
    /// Visits a <see cref="NavigationRegionSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="NavigationRegionSyntax"/> to visit.</param>
    public virtual void VisitNavigationRegion(NavigationRegionSyntax syntax)
    {
        VisitNode(syntax);

        if (syntax.Logo is not null)
        {
            VisitNavigationLogo(syntax.Logo);
        }

        foreach (var item in syntax.Items)
        {
            VisitNavigationItem(item);
        }
    }

    /// <summary>
    /// Visits a <see cref="NavigationLogoSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="NavigationLogoSyntax"/> to visit.</param>
    public virtual void VisitNavigationLogo(NavigationLogoSyntax syntax) => VisitNode(syntax);

    /// <summary>
    /// Visits a <see cref="NavigationItemSyntax"/> node and its children.
    /// </summary>
    /// <param name="syntax">The <see cref="NavigationItemSyntax"/> to visit.</param>
    public virtual void VisitNavigationItem(NavigationItemSyntax syntax)
    {
        VisitNode(syntax);

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
    /// Visits a <see cref="NavigationTargetSyntax"/> node.
    /// </summary>
    /// <param name="syntax">The <see cref="NavigationTargetSyntax"/> to visit.</param>
    public virtual void VisitNavigationTarget(NavigationTargetSyntax syntax) => VisitNode(syntax);
}

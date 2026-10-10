// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines where a navigation item opens. Mirrors Scene's <c>DestinationKind</c>.
/// </summary>
public enum ContributionDestinationKind
{
    /// <summary>
    /// The item opens in a named outlet.
    /// </summary>
    Outlet = 0,

    /// <summary>
    /// The item opens a dialog.
    /// </summary>
    Dialog = 1,

    /// <summary>
    /// The item opens an external route.
    /// </summary>
    External = 2
}

/// <summary>
/// Represents a <c>contribute to &lt;ContributionPoint&gt;</c> declaration - one item contributed into a
/// named contribution point a <c>layout</c> template's slot accepts.
/// </summary>
/// <param name="ContributionPoint">The name of the contribution point this contributes to.</param>
/// <param name="Navigate">The optional <see cref="ScreenNavigateSyntax"/> the contribution carries.</param>
/// <param name="Label">The optional display label.</param>
/// <param name="Order">The optional sort order, lower first.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A contribution may sit anywhere in the module/feature tree - directly on a <see cref="ModuleSyntax"/> or
/// on a <see cref="FeatureSyntax"/> at any nesting depth. It resolves to the nearest enclosing
/// <see cref="LayoutSyntax"/> or <see cref="ScreenTemplateSyntax"/> slot whose <see cref="SlotSyntax.Contributes"/> names the same contribution
/// point: the module the contribution sits in first, then every other module in the document.
/// </remarks>
public record ContributionSyntax(
    string ContributionPoint,
    ScreenNavigateSyntax? Navigate,
    string? Label,
    int? Order,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the exact stable id of the navigation item, or <c>null</c>. Maps to Scene's <c>NavigationItem.Id</c>.
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// Gets the icon name, or <c>null</c>. Maps to Scene's <c>NavigationItem.Icon</c>.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the presentation hint, or <c>null</c>. Maps to Scene's <c>NavigationItem.Presentation</c>.
    /// </summary>
    public string? Presentation { get; init; }

    /// <summary>
    /// Gets the group the item belongs to, or <c>null</c>. Maps to Scene's <c>NavigationItem.Group</c>.
    /// </summary>
    public string? Group { get; init; }

    /// <summary>
    /// Gets where the item opens, or <c>null</c> for the default outlet. Maps to Scene's <c>NavigationItem.Destination</c>.
    /// </summary>
    public ContributionDestinationSyntax? Destination { get; init; }
}

/// <summary>
/// Represents a <c>destination outlet|dialog|external &lt;target&gt;</c> line in a contribution.
/// </summary>
/// <param name="Kind">The <see cref="ContributionDestinationKind"/>.</param>
/// <param name="Target">The outlet name, dialog name, or external route.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ContributionDestinationSyntax(ContributionDestinationKind Kind, string Target, SourceLocation Location) : SyntaxNode(Location);

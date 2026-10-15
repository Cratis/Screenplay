// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents which navigation region of the application shell a <c>navigation</c> block configures.
/// </summary>
public enum NavigationRegionKind
{
    /// <summary>
    /// The top bar - a flat row of items.
    /// </summary>
    Topbar = 0,

    /// <summary>
    /// The sidebar - items that may nest sub-items.
    /// </summary>
    Sidebar = 1
}

/// <summary>
/// Represents how a module's navigation region relates to the application's region of the same kind.
/// </summary>
public enum NavigationRegionMode
{
    /// <summary>
    /// The region states no mode - it is the application's own base region, declared in a <c>layout</c>.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// The module shows the application's items and adds none (<c>inherits</c>). The default when a module declares nothing.
    /// </summary>
    Inherit = 1,

    /// <summary>
    /// The module shows the application's items followed by its own (<c>extends</c>).
    /// </summary>
    Extend = 2,

    /// <summary>
    /// The module shows only its own items (<c>overrides</c>).
    /// </summary>
    Override = 3
}

/// <summary>
/// Represents a <c>navigation topbar</c> or <c>navigation sidebar</c> block - one region of the application shell's
/// navigation, declared by a <c>layout</c> for the application and by a <c>module</c> for what it changes.
/// </summary>
/// <param name="Region">Which region the block configures.</param>
/// <param name="Mode">How the region relates to the application's - <see cref="NavigationRegionMode.Unspecified"/> in a layout.</param>
/// <param name="Items">The <see cref="NavigationItemSyntax">items</see> of the region, in authored order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// The compiler form of Scene's <c>NavigationRegion</c>. A module that declares no block for a region inherits
/// the application's. The older <c>contribute to Navigation</c> form stays valid; the two are independent
/// mechanisms - a block describes the region's own item tree, a contribution adds one flat item to a
/// contribution point.
/// </remarks>
public record NavigationRegionSyntax(
    NavigationRegionKind Region,
    NavigationRegionMode Mode,
    IEnumerable<NavigationItemSyntax> Items,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the application logo declared in this region block, or <c>null</c>. Application level only.
    /// </summary>
    public NavigationLogoSyntax? Logo { get; init; }
}

/// <summary>
/// Represents an <c>item &lt;id&gt; title "..." [icon X] [target Module[.Feature]] [slot name]</c> line of a navigation region.
/// </summary>
/// <param name="Id">The id of the item, unique within its region.</param>
/// <param name="Title">The title shown, as written - a quoted string's text or a <c>$strings.&lt;key&gt;</c> token.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record NavigationItemSyntax(string Id, string Title, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the icon name, or <c>null</c>.
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// Gets the module or feature the item navigates to, or <c>null</c> for a grouping item that only has children.
    /// </summary>
    public NavigationTargetSyntax? Target { get; init; }

    /// <summary>
    /// Gets the rendering slot the target renders into, or <c>null</c> for the layout's primary content slot.
    /// </summary>
    public string? Slot { get; init; }

    /// <summary>
    /// Gets the nested sub-items - sidebar only.
    /// </summary>
    public IEnumerable<NavigationItemSyntax> Children { get; init; } = [];
}

/// <summary>
/// Represents the <c>target Module[.Feature[.SubFeature]]</c> of a navigation item.
/// </summary>
/// <param name="Module">The name of the module.</param>
/// <param name="Feature">The dotted feature path inside the module, or <c>null</c> to go to the module's first feature.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record NavigationTargetSyntax(string Module, string? Feature, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>logo image "..." title "..."</c> line of the application's navigation.
/// </summary>
/// <param name="Image">The image the logo shows.</param>
/// <param name="Title">The application title shown with the logo, as written.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record NavigationLogoSyntax(string Image, string Title, SourceLocation Location) : SyntaxNode(Location);

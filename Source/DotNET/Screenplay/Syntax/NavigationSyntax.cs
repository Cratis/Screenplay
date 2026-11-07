// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an <c>item &lt;id&gt; title "..." [icon X] [target Module[.Feature]] [slot name]</c> line of a navigation outlet.
/// </summary>
/// <param name="Id">The id of the item, unique within its outlet.</param>
/// <param name="Title">The title shown, as written - a quoted string's text or a <c>$strings.&lt;key&gt;</c> token.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record NavigationItemSyntax(string Id, string Title, SourceLocation Location) : OutletItemSyntax(Id, Title, Location)
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
    /// Gets the nested sub-items.
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

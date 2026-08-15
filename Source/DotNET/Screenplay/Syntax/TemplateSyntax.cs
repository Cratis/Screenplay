// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// The two ways a <see cref="LayoutSyntax"/> arranges content within its slots.
/// </summary>
public enum LayoutArrangement
{
    /// <summary>
    /// Content reflows within a responsive row/column/grid template, with overrides per width/height size class.
    /// </summary>
    Flow = 0,

    /// <summary>
    /// Content is placed at pixel-precise coordinates, one variant per width/height size-class combination.
    /// </summary>
    Freeform = 1,
}

/// <summary>
/// The container primitives a <see cref="TemplateSyntax"/> tree nests <see cref="TemplateSlotSyntax"/> leaves within.
/// </summary>
public enum TemplateContainerKind
{
    /// <summary>
    /// An unordered group of children - the implicit shape of a <c>template</c> or <c>when</c> body with no explicit row/column/grid nesting.
    /// </summary>
    Flat = 0,

    /// <summary>
    /// Children are arranged horizontally.
    /// </summary>
    Row = 1,

    /// <summary>
    /// Children are arranged vertically.
    /// </summary>
    Column = 2,

    /// <summary>
    /// Children are arranged in a two-dimensional grid.
    /// </summary>
    Grid = 3,
}

/// <summary>
/// Represents the <c>template</c> block of a <see cref="LayoutSyntax"/> with <see cref="Syntax.LayoutArrangement.Flow"/> arrangement.
/// </summary>
/// <param name="Root">The root <see cref="TemplateNodeSyntax"/> of the template tree.</param>
/// <param name="Overrides">The <c>when</c> overrides that replace the root tree for a given width/height size class.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TemplateSyntax(TemplateNodeSyntax Root, IEnumerable<TemplateOverrideSyntax> Overrides, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Base type for a node within a <see cref="TemplateSyntax"/> tree - either a <see cref="TemplateContainerSyntax"/> or a <see cref="TemplateSlotSyntax"/> leaf.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record TemplateNodeSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>row</c>, <c>column</c> or <c>grid</c> container within a template tree.
/// </summary>
/// <param name="Kind">The <see cref="TemplateContainerKind"/> this container arranges its children by.</param>
/// <param name="Children">The child <see cref="TemplateNodeSyntax"/> nodes, in declaration order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Gap">The spacing between children, or <c>null</c> if not declared.</param>
public record TemplateContainerSyntax(
    TemplateContainerKind Kind,
    IEnumerable<TemplateNodeSyntax> Children,
    SourceLocation Location,
    int? Gap = null) : TemplateNodeSyntax(Location);

/// <summary>
/// Represents a slot placed as a leaf within a template tree.
/// </summary>
/// <param name="Name">The slot's name.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Contributes">The name of the contribution point this slot accepts contributions for, or <c>null</c> if it does not accept any.</param>
/// <param name="Width">The slot's fixed size along its container's main axis, or <c>null</c> if not declared.</param>
/// <param name="Height">The slot's fixed size along its container's cross axis, or <c>null</c> if not declared.</param>
/// <param name="Grow">Whether the slot grows to fill the remaining space in its container.</param>
/// <param name="Span">The number of grid tracks the slot spans, or <c>null</c> if not declared.</param>
public record TemplateSlotSyntax(
    string Name,
    SourceLocation Location,
    string? Contributes = null,
    int? Width = null,
    int? Height = null,
    bool Grow = false,
    int? Span = null) : TemplateNodeSyntax(Location);

/// <summary>
/// Represents a <c>when</c> override that replaces a <see cref="TemplateSyntax"/>'s root tree for a given width/height size class.
/// </summary>
/// <param name="Width">The width size class this override targets, or <c>null</c> if it targets any width.</param>
/// <param name="Height">The height size class this override targets, or <c>null</c> if it targets any height.</param>
/// <param name="Root">The replacement <see cref="TemplateNodeSyntax"/> tree.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TemplateOverrideSyntax(string? Width, string? Height, TemplateNodeSyntax Root, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>variant</c> block of a <see cref="LayoutSyntax"/> with <see cref="Syntax.LayoutArrangement.Freeform"/> arrangement -
/// the pixel-precise placement of every slot for one width/height size-class combination.
/// </summary>
/// <param name="Width">The width size class this variant targets.</param>
/// <param name="Height">The height size class this variant targets.</param>
/// <param name="Places">The <see cref="PlaceSyntax"/> placements declared in this variant.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record VariantSyntax(string Width, string Height, IEnumerable<PlaceSyntax> Places, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a single <c>place</c> directive within a <see cref="VariantSyntax"/>.
/// </summary>
/// <param name="SlotName">The name of the slot being placed.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Hidden">Whether the slot is hidden in this variant.</param>
/// <param name="X">The slot's horizontal position, or <c>null</c> when <see cref="Hidden"/>.</param>
/// <param name="Y">The slot's vertical position, or <c>null</c> when <see cref="Hidden"/>.</param>
/// <param name="SizeWidth">The slot's width - a pixel count, or <c>"fill"</c> - or <c>null</c> when <see cref="Hidden"/>.</param>
/// <param name="SizeHeight">The slot's height - a pixel count, or <c>"fill"</c> - or <c>null</c> when <see cref="Hidden"/>.</param>
public record PlaceSyntax(
    string SlotName,
    SourceLocation Location,
    bool Hidden = false,
    int? X = null,
    int? Y = null,
    string? SizeWidth = null,
    string? SizeHeight = null) : SyntaxNode(Location);

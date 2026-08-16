// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// The two ways an <see cref="ArrangementSyntax"/> arranges the slots of a layout, screen template or dialog template.
/// </summary>
public enum ArrangementMode
{
    /// <summary>
    /// Content reflows within a responsive row/column/grid tree, with overrides per width/height size class.
    /// </summary>
    Flow = 0,

    /// <summary>
    /// Content is placed at pixel-precise coordinates, one variant per width/height size-class combination.
    /// </summary>
    Freeform = 1,
}

/// <summary>
/// The container primitives an <see cref="ArrangementSyntax"/> tree nests <see cref="ArrangementSlotSyntax"/> leaves within.
/// </summary>
public enum ArrangementContainerKind
{
    /// <summary>
    /// An unordered group of children - the implicit shape of an <c>arrangement</c> or <c>when</c> body with no explicit row/column/grid nesting.
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
/// Represents the <c>arrangement</c> block of a <see cref="LayoutSyntax"/>, <see cref="ScreenTemplateSyntax"/> or
/// <see cref="DialogTemplateSyntax"/> - how the slots it declares share the available space.
/// </summary>
/// <param name="Mode">The <see cref="ArrangementMode"/> the block arranges by.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Root">The root <see cref="ArrangementNodeSyntax"/> of the tree, present when <see cref="Mode"/> is <see cref="ArrangementMode.Flow"/>.</param>
/// <param name="Overrides">The <c>when</c> overrides replacing the root tree for a given size class, present when <see cref="Mode"/> is <see cref="ArrangementMode.Flow"/>.</param>
/// <param name="Variants">The <see cref="VariantSyntax">variants</see>, present when <see cref="Mode"/> is <see cref="ArrangementMode.Freeform"/>.</param>
/// <remarks>
/// The block holds the tree directly - there is no intermediate <c>template</c> block, so the word <c>template</c>
/// means a screen or dialog template throughout the language and nothing else.
/// </remarks>
public record ArrangementSyntax(
    ArrangementMode Mode,
    SourceLocation Location,
    ArrangementNodeSyntax? Root = null,
    IEnumerable<ArrangementOverrideSyntax>? Overrides = null,
    IEnumerable<VariantSyntax>? Variants = null) : SyntaxNode(Location);

/// <summary>
/// Base type for a node within an <see cref="ArrangementSyntax"/> tree - either an <see cref="ArrangementContainerSyntax"/> or an <see cref="ArrangementSlotSyntax"/> leaf.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record ArrangementNodeSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>row</c>, <c>column</c> or <c>grid</c> container within an arrangement tree.
/// </summary>
/// <param name="Kind">The <see cref="ArrangementContainerKind"/> this container arranges its children by.</param>
/// <param name="Children">The child <see cref="ArrangementNodeSyntax"/> nodes, in declaration order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Gap">The spacing between children, or <c>null</c> if not declared.</param>
public record ArrangementContainerSyntax(
    ArrangementContainerKind Kind,
    IEnumerable<ArrangementNodeSyntax> Children,
    SourceLocation Location,
    int? Gap = null) : ArrangementNodeSyntax(Location);

/// <summary>
/// Represents a slot positioned as a leaf within an arrangement tree.
/// </summary>
/// <param name="Name">The slot's name.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Width">The slot's fixed size along its container's main axis, or <c>null</c> if not declared.</param>
/// <param name="Height">The slot's fixed size along its container's cross axis, or <c>null</c> if not declared.</param>
/// <param name="Grow">Whether the slot grows to fill the remaining space in its container.</param>
/// <param name="Span">The number of grid tracks the slot spans, or <c>null</c> if not declared.</param>
/// <remarks>
/// A leaf carries sizing only. What a slot <em>is</em> - its name, and the contribution point it accepts - is
/// stated once by the <see cref="SlotSyntax">slot declaration</see> in the body, so a slot never says two
/// different things about itself in two places.
/// </remarks>
public record ArrangementSlotSyntax(
    string Name,
    SourceLocation Location,
    int? Width = null,
    int? Height = null,
    bool Grow = false,
    int? Span = null) : ArrangementNodeSyntax(Location);

/// <summary>
/// Represents a <c>when</c> override that replaces an <see cref="ArrangementSyntax"/>'s root tree for a given width/height size class.
/// </summary>
/// <param name="Width">The width size class this override targets, or <c>null</c> if it targets any width.</param>
/// <param name="Height">The height size class this override targets, or <c>null</c> if it targets any height.</param>
/// <param name="Root">The replacement <see cref="ArrangementNodeSyntax"/> tree.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ArrangementOverrideSyntax(string? Width, string? Height, ArrangementNodeSyntax Root, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>variant</c> block of an <see cref="ArrangementSyntax"/> with <see cref="ArrangementMode.Freeform"/> mode -
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

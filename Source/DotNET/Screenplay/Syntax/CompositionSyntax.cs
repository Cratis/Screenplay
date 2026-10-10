// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top level <c>exposure for &lt;Owner&gt;</c> declaration - what a layout or template lets whatever
/// sits inside it configure. Mirrors Scene's <c>ExposureDeclaration</c>.
/// </summary>
/// <param name="Owner">The name of the layout, screen template or dialog template that declares the exposure.</param>
/// <param name="Properties">The <see cref="ExposedPropertySyntax">properties</see> it exposes, in declaration order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ExposureSyntax(string Owner, IEnumerable<ExposedPropertySyntax> Properties, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one <c>property &lt;component&gt;.&lt;path&gt;</c> line in an <see cref="ExposureSyntax"/>. Mirrors Scene's
/// <c>ExposedProperty</c>.
/// </summary>
/// <param name="Component">The exact stable id of the component in the owner that has the property.</param>
/// <param name="Path">The path of the property on the component.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ExposedPropertySyntax(string Component, string Path, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the label the consumer sees instead of the property's own, or <c>null</c>.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets a value indicating whether the property is exposed as a collection - an <c>operations</c> clause is present.
    /// </summary>
    public bool IsCollection { get; init; }

    /// <summary>
    /// Gets the collection operations a consumer may perform - <c>add</c>, <c>remove</c>, <c>reorder</c> and
    /// <c>edit-fields</c>. Empty for a collection exposed with <c>operations none</c>.
    /// </summary>
    public IEnumerable<string> Operations { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether a <c>fields</c> clause restricts which item fields a consumer may change.
    /// </summary>
    public bool RestrictsFields { get; init; }

    /// <summary>
    /// Gets the collection item fields a consumer may change when <see cref="RestrictsFields"/> is set; empty for <c>fields none</c>.
    /// </summary>
    public IEnumerable<string> EditableFields { get; init; } = [];

    /// <summary>
    /// Gets the owner whose exposure this one passes on, or <c>null</c> when it is the owner's own exposure.
    /// </summary>
    public string? ReExposes { get; init; }
}

/// <summary>
/// Represents a top level <c>instance &lt;Instance&gt;</c> block - the values a screen, or a template nested in an
/// owner's slot, stores for what was exposed to it. Mirrors Scene's <c>InstanceContribution</c> list, grouped by instance.
/// </summary>
/// <param name="Instance">The screen or template that stores the values.</param>
/// <param name="Contributions">The <see cref="InstanceContributionSyntax">contributions</see>, in the order stored.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record InstanceContributionsSyntax(string Instance, IEnumerable<InstanceContributionSyntax> Contributions, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one <c>set</c> or <c>items</c> line in an <see cref="InstanceContributionsSyntax"/>.
/// </summary>
/// <param name="Component">The exact stable id of the component the value is for.</param>
/// <param name="Path">The path of the property on the component.</param>
/// <param name="Value">The value for a <c>set</c> line, or <c>null</c> for an <c>items</c> block.</param>
/// <param name="Items">The <see cref="ContributedItemSyntax">items</see> an <c>items</c> block adds; empty for a <c>set</c> line.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record InstanceContributionSyntax(
    string Component,
    string Path,
    ExpressionSyntax? Value,
    IEnumerable<ContributedItemSyntax> Items,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one <c>item &lt;id&gt;</c> block an instance adds to an exposed collection. Mirrors Scene's <c>ContributedItem</c>.
/// </summary>
/// <param name="Id">The item's exact id.</param>
/// <param name="Values">The <see cref="ContributedItemValueSyntax">field values</see>, in authored order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ContributedItemSyntax(string Id, IEnumerable<ContributedItemValueSyntax> Values, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one <c>&lt;field&gt; = &lt;value&gt;</c> line in a <see cref="ContributedItemSyntax"/>.
/// </summary>
/// <param name="Field">The item field.</param>
/// <param name="Value">The typed literal value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ContributedItemValueSyntax(string Field, ExpressionSyntax Value, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>contribute to &lt;Point&gt; [order &lt;n&gt;]</c> block in a screen - content the screen contributes to
/// a contribution point elsewhere in the tree. Mirrors Scene's screen <c>Contribution</c>.
/// </summary>
/// <param name="ContributionPoint">The contribution point's name.</param>
/// <param name="Order">The optional order among the point's contributions.</param>
/// <param name="Directives">The contributed <see cref="ScreenDirectiveSyntax">content</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenContributionSyntax(
    string ContributionPoint,
    int? Order,
    IEnumerable<ScreenDirectiveSyntax> Directives,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>content &lt;slot&gt;</c> block in a screen or dialog template - chrome the template itself provides
/// for one of its slots. Mirrors Scene's template <c>Content</c>.
/// </summary>
/// <param name="Slot">The slot the content fills.</param>
/// <param name="Directives">The <see cref="ScreenDirectiveSyntax">content</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TemplateSlotContentSyntax(string Slot, IEnumerable<ScreenDirectiveSyntax> Directives, SourceLocation Location) : SyntaxNode(Location);

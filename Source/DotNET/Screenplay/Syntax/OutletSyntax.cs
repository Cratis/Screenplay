// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an <c>outlet</c> - a named list of items a layout or template offers, with the items it shows by default.
/// </summary>
/// <param name="Name">The name of the outlet, unique within the template that declares it.</param>
/// <param name="Kind">The <see cref="OutletKind"/> on a declaration (<c>outlet &lt;name&gt; of &lt;kind&gt;</c>), or <c>null</c> on a lower level, which takes the kind of the outlet it changes.</param>
/// <param name="Mode">How a lower level combines its items with the inherited ones; <see cref="OutletMode.Unspecified"/> on a declaration.</param>
/// <param name="Items">The items, in the order written: the defaults on a declaration, the own items on a lower level.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A layout, screen template or dialog template declares <c>outlet &lt;name&gt; of actions|filters|tabs|stats|navigation</c>
/// and lists its <c>default</c> items. A module, feature or slice screen writes <c>outlet &lt;name&gt; inherits|extends|overrides</c>
/// to change the list for the screens beneath it, and may <c>hide</c> an inherited item or place its own <c>before</c> or
/// <c>after</c> one. Like every screen construct, an outlet is authoring syntax: the executable model does not carry it.
/// </remarks>
public record OutletSyntax(string Name, OutletKind? Kind, OutletMode Mode, IEnumerable<OutletItemSyntax> Items, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets what the outlet's items act on - <c>for item</c> or <c>for screen</c> - or <c>null</c> when the declaration does not say.
    /// </summary>
    public ScreenActionSubjectKind? Subject { get; init; }

    /// <summary>
    /// Gets the slot the outlet belongs to - <c>in &lt;slot&gt;</c> - or <c>null</c> when it is not tied to one.
    /// </summary>
    public string? Slot { get; init; }

    /// <summary>
    /// Gets the inherited items a lower level hides - <c>hide &lt;id&gt;</c>.
    /// </summary>
    public IEnumerable<OutletHideSyntax> Hides { get; init; } = [];

    /// <summary>
    /// Gets the application's logo, declared once in a layout's navigation outlet, or <c>null</c>.
    /// </summary>
    public NavigationLogoSyntax? Logo { get; init; }
}

/// <summary>
/// Represents <c>hide &lt;id&gt;</c> - an inherited outlet item a lower level does not show.
/// </summary>
/// <param name="Id">The id of the inherited item.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OutletHideSyntax(string Id, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents <c>before &lt;id&gt;</c> or <c>after &lt;id&gt;</c> under an outlet item - where the item stands among the inherited ones.
/// </summary>
/// <param name="Placement">Whether the item stands before or after the named one.</param>
/// <param name="Id">The id of the item it stands next to.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OutletPositionSyntax(OutletPlacement Placement, string Id, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one item of an <see cref="OutletSyntax"/>. The outlet's <see cref="OutletKind"/> decides which item forms it takes.
/// </summary>
/// <param name="Id">The id of the item, unique within the outlet.</param>
/// <param name="Title">The title shown, as written - a quoted string's text or a <c>$strings.&lt;key&gt;</c> token.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record OutletItemSyntax(string Id, string Title, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets a value indicating whether the item is one of the outlet's defaults - <c>default &lt;id&gt; …</c> on a declaration.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Gets where the item stands among the inherited ones, or <c>null</c> for after them.
    /// </summary>
    public OutletPositionSyntax? Position { get; init; }
}

/// <summary>
/// Represents <c>group &lt;id&gt; title "…"</c> in an actions outlet - a menu of buttons.
/// </summary>
/// <param name="Id">The id of the group, unique within the outlet.</param>
/// <param name="Title">The title shown, as written.</param>
/// <param name="Buttons">The buttons the group holds, in the order written.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OutletGroupSyntax(string Id, string Title, IEnumerable<ToolbarButtonSyntax> Buttons, SourceLocation Location) : OutletItemSyntax(Id, Title, Location)
{
    /// <summary>
    /// Gets the icon shown on the group, or <c>null</c>.
    /// </summary>
    public string? Icon { get; init; }
}

/// <summary>
/// Represents <c>filter &lt;id&gt; title "…" [sets &lt;Query&gt;.&lt;param&gt;]</c> in a filters outlet - a control that sets a parameter of the query the screen shows.
/// </summary>
/// <param name="Id">The id of the filter, unique within the outlet.</param>
/// <param name="Title">The title shown, as written.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OutletFilterSyntax(string Id, string Title, SourceLocation Location) : OutletItemSyntax(Id, Title, Location)
{
    /// <summary>
    /// Gets the query whose parameter the filter sets, or <c>null</c> for a filter that binds to nothing yet.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Gets the parameter the filter sets, or <c>null</c>.
    /// </summary>
    public string? Parameter { get; init; }
}

/// <summary>
/// Represents <c>tab &lt;id&gt; title "…" [shows &lt;Screen&gt;]</c> in a tabs outlet - a screen shown in the slot the tabs own.
/// </summary>
/// <param name="Id">The id of the tab, unique within the outlet.</param>
/// <param name="Title">The title shown, as written.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OutletTabSyntax(string Id, string Title, SourceLocation Location) : OutletItemSyntax(Id, Title, Location)
{
    /// <summary>
    /// Gets the screen the tab shows, or <c>null</c>.
    /// </summary>
    public string? Shows { get; init; }
}

/// <summary>
/// Represents <c>stat &lt;id&gt; title "…" [value from &lt;Query&gt;.&lt;field&gt;]</c> in a stats outlet - a figure read from a single-result query.
/// </summary>
/// <param name="Id">The id of the stat, unique within the outlet.</param>
/// <param name="Title">The title shown, as written.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record OutletStatSyntax(string Id, string Title, SourceLocation Location) : OutletItemSyntax(Id, Title, Location)
{
    /// <summary>
    /// Gets the query the value is read from, or <c>null</c>.
    /// </summary>
    public string? Query { get; init; }

    /// <summary>
    /// Gets the field of the query's result the value is read from, or <c>null</c>.
    /// </summary>
    public string? Field { get; init; }
}

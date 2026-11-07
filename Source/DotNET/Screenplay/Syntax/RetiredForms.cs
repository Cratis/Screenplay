// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a form an earlier Screenplay version wrote that the current language replaced. The parser reports the
/// form as an error and keeps what it read here, so a migration can rewrite the tree instead of the text.
/// </summary>
/// <remarks>
/// A retired form is source metadata: it is not typed syntax, is never serialized and never printed. A document
/// without retired forms is a document of the current language.
/// </remarks>
/// <param name="Location">The <see cref="SourceLocation"/> of the line that writes the retired form.</param>
public abstract record RetiredForm(SourceLocation Location);

/// <summary>
/// Represents a Screenplay 5 <c>outlet &lt;name&gt;</c> that names a slot. The tree already holds the slot.
/// </summary>
/// <param name="Place">Where the outlet was written.</param>
/// <param name="Name">The name of the slot.</param>
/// <param name="Replacement">The node the line is written as since Screenplay 6.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the line.</param>
public sealed record RetiredOutlet(RetiredOutletPlace Place, string Name, SyntaxNode Replacement, SourceLocation Location) : RetiredForm(Location);

/// <summary>
/// Represents a Screenplay 5 <c>navigation topbar|sidebar [mode]</c> region on a layout, template or module.
/// </summary>
/// <param name="Region">The region - <c>topbar</c> or <c>sidebar</c>.</param>
/// <param name="Mode">The mode a module wrote, or <c>null</c> when it wrote none.</param>
/// <param name="Logo">The <c>logo</c> the region holds, if any.</param>
/// <param name="Items">The <c>item</c> and <c>button</c> lines the region holds, in authored order.</param>
/// <param name="Unrecognized">The lines in the region that are neither an item, a button nor a logo.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the region header.</param>
public sealed record RetiredNavigationRegion(
    string Region,
    OutletMode? Mode,
    NavigationLogoSyntax? Logo,
    IReadOnlyList<OutletItemSyntax> Items,
    IReadOnlyList<SourceLocation> Unrecognized,
    SourceLocation Location) : RetiredForm(Location);

/// <summary>
/// Represents a Screenplay 5 <c>&lt;slot&gt; contributes &lt;Point&gt;</c> slot declaration. The tree already holds the slot.
/// </summary>
/// <param name="Slot">The slot, without the contribution.</param>
/// <param name="Point">The contribution point the slot opened.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the line.</param>
public sealed record RetiredSlotContribution(SlotSyntax Slot, string Point, SourceLocation Location) : RetiredForm(Location);

/// <summary>
/// Represents a Screenplay 5 <c>toolbar &lt;name&gt;</c> on a feature or module.
/// </summary>
/// <param name="Toolbar">The toolbar as the parser read it, with its items and buttons.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the toolbar header.</param>
public sealed record RetiredToolbar(ScreenToolbarSyntax Toolbar, SourceLocation Location) : RetiredForm(Location);

/// <summary>
/// Represents a slot declaration in a layout or template that holds items under it - a toolbar slot with its buttons.
/// </summary>
/// <param name="Slot">The slot, without the items.</param>
/// <param name="Toolbar">A <c>toolbar &lt;name&gt;</c> the slot holds, with its items and buttons, or <c>null</c> when it holds none.</param>
/// <param name="Buttons">The <c>button</c> lines written directly in the slot.</param>
/// <param name="Others">The lines that are neither - a component, for example.</param>
/// <param name="Location">The <see cref="SourceLocation"/> of the slot line.</param>
public sealed record RetiredSlotItems(
    SlotSyntax Slot,
    ScreenToolbarSyntax? Toolbar,
    IReadOnlyList<ToolbarButtonSyntax> Buttons,
    IReadOnlyList<SourceLocation> Others,
    SourceLocation Location) : RetiredForm(Location);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top level <c>layout &lt;Name&gt;</c> block - the application's base navigational look: the
/// shell holding a top bar, a navigation region, a content region and a footer.
/// </summary>
/// <param name="Name">The name of the layout.</param>
/// <param name="Slots">The <see cref="SlotSyntax">slots</see> the layout declares.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Arrangement">The <see cref="ArrangementSyntax"/> arranging the slots, or <c>null</c> when the layout only names them.</param>
/// <remarks>
/// An application has one base look and selects it - a <see cref="UiProfileSyntax"/> names the layout its
/// build renders inside, the same way it names its <see cref="ThemeSyntax"/>. Several may be declared so
/// that different profiles can select different shells; each profile still selects exactly one.
/// <para>
/// A layout's slots are the outermost thing a <see cref="ScreenTemplateSyntax"/> can fit into: a module's
/// screen template names one of them with <c>fits slot</c>, and templates nest from there by that same rule.
/// This is also where an application-wide contribution point such as <c>Navigation</c> belongs.
/// </para>
/// </remarks>
public record LayoutSyntax(
    string Name,
    IEnumerable<SlotSyntax> Slots,
    SourceLocation Location,
    ArrangementSyntax? Arrangement = null) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>screen template &lt;Name&gt;</c> declaration - a reusable shape that goes inside the
/// application's shell, at module, feature or slice level.
/// </summary>
/// <param name="Name">The name of the screen template.</param>
/// <param name="Slots">The <see cref="SlotSyntax">slots</see> the screen template declares.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="FitsSlot">The name of the slot on the parent structure this template fills, or <c>null</c> if it does not say.</param>
/// <param name="Arrangement">The <see cref="ArrangementSyntax"/> arranging the slots, or <c>null</c> when the template only names them.</param>
/// <remarks>
/// An application has many screen templates, and <see cref="FitsSlot"/> is the single rule that lets them
/// nest arbitrarily deep: a module's template fits a slot on the application <see cref="LayoutSyntax"/>, a
/// feature's fits a slot the module's template declares, a slice's fits one the feature's declares.
/// </remarks>
public record ScreenTemplateSyntax(
    string Name,
    IEnumerable<SlotSyntax> Slots,
    SourceLocation Location,
    string? FitsSlot = null,
    ArrangementSyntax? Arrangement = null) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>dialog template &lt;Name&gt;</c> declaration - a reusable shape for content that opens
/// over the application rather than inside its shell.
/// </summary>
/// <param name="Name">The name of the dialog template.</param>
/// <param name="Slots">The <see cref="SlotSyntax">slots</see> the dialog template declares.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Arrangement">The <see cref="ArrangementSyntax"/> arranging the slots, or <c>null</c> when the template only names them.</param>
/// <remarks>
/// A dialog template is a <see cref="ScreenTemplateSyntax"/> in everything but one respect: it has no
/// <c>fits slot</c>, because a dialog occupies no slot of the structure it opens over.
/// </remarks>
public record DialogTemplateSyntax(
    string Name,
    IEnumerable<SlotSyntax> Slots,
    SourceLocation Location,
    ArrangementSyntax? Arrangement = null) : SyntaxNode(Location);

/// <summary>
/// Represents one named slot of a <see cref="LayoutSyntax"/>, <see cref="ScreenTemplateSyntax"/> or <see cref="DialogTemplateSyntax"/>.
/// </summary>
/// <param name="Name">The name of the slot.</param>
/// <param name="Contributes">The name of the contribution point this slot accepts contributions for, or <c>null</c> if it does not accept any.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SlotSyntax(string Name, string? Contributes, SourceLocation Location) : SyntaxNode(Location);

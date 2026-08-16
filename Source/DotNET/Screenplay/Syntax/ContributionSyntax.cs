// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

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
    SourceLocation Location) : SyntaxNode(Location);

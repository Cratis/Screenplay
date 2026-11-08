// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top level <c>theme &lt;Name&gt;</c> block declaring a named visual theme: the component packages it is
/// meaningful against and, when it <c>implements</c> a <see cref="DesignSystemSyntax"/>, the values of that design system's tokens.
/// </summary>
/// <param name="Name">The theme's name.</param>
/// <param name="CompatibleWith">The component packages this theme is declared compatible with.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A theme is only meaningful relative to a specific set of component packages - an arbitrary
/// theme/package pairing can silently produce unstyled or broken components. A <see cref="UiProfileSyntax"/>
/// selecting a theme not declared compatible with one of its own <see cref="UiProfileSyntax.Packages"/> is a
/// compile-time warning, not a hard error - the pairing might still work by coincidence, but the gap is made
/// visible the same way an ambiguous or unknown name already is.
/// </remarks>
public record ThemeSyntax(string Name, IEnumerable<string> CompatibleWith, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the name of the <see cref="DesignSystemSyntax"/> this theme implements, or <c>null</c> for a compatibility-only theme.
    /// </summary>
    public string? Implements { get; init; }

    /// <summary>
    /// Gets the <c>palette</c>: the raw brand values token values refer to as <c>palette.&lt;name&gt;</c>.
    /// </summary>
    public IEnumerable<DesignAssignmentSyntax> Palette { get; init; } = [];

    /// <summary>
    /// Gets the mode-independent <c>values</c>: a value per token address.
    /// </summary>
    public IEnumerable<DesignAssignmentSyntax> Values { get; init; } = [];

    /// <summary>
    /// Gets the per-mode blocks that give each color token its value in that mode.
    /// </summary>
    public IEnumerable<ThemeModeValuesSyntax> ModeValues { get; init; } = [];
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top level <c>ui profile &lt;Name&gt;</c> block declaring a named target: the platform(s) it
/// runs on, its default size class, and the component packages a build resolves widget names against.
/// </summary>
/// <param name="Name">The profile's name.</param>
/// <param name="Platforms">The platform(s) this profile targets, e.g. <c>web</c>, <c>ios</c>, <c>android</c>.</param>
/// <param name="DefaultSizeClass">The size class assumed when nothing more specific overrides it, or <c>null</c> if not declared.</param>
/// <param name="Packages">The component packages this profile draws from, in override-priority order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Theme">The name of the <see cref="ThemeSyntax"/> this profile applies, or <c>null</c> if not declared.</param>
/// <param name="Layout">The name of the <see cref="LayoutSyntax"/> this profile renders inside, or <c>null</c> if not declared.</param>
/// <remarks>
/// A <c>screen</c> never declares which profile it targets - profile selection is a build/Stage concern, so
/// the same screen resolves against different package chains per build. <c>core</c>, the built-in vocabulary,
/// is always the final fallback regardless of what a profile lists in <see cref="Packages"/>.
/// </remarks>
public record UiProfileSyntax(
    string Name,
    IEnumerable<string> Platforms,
    string? DefaultSizeClass,
    IEnumerable<string> Packages,
    SourceLocation Location,
    string? Theme = null,
    string? Layout = null) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the selected icon sets or packages available to this profile.
    /// </summary>
    public IEnumerable<string> Icons { get; init; } = [];

    /// <summary>
    /// Gets the <c>root template</c> the whole application is drawn in, or <c>null</c> when the profile declares none (the legacy <see cref="Layout"/> may still name one).
    /// </summary>
    public RootTemplateSyntax? Root { get; init; }

    /// <summary>
    /// Gets the <c>appearance</c> block (colour scheme and accent), or <c>null</c> for none.
    /// </summary>
    public ProfileAppearanceSyntax? Appearance { get; init; }

    /// <summary>
    /// Gets the <c>new screens use template</c> default, or <c>null</c> for none.
    /// </summary>
    public NewScreenTemplateSyntax? NewScreenTemplate { get; init; }

    /// <summary>
    /// Gets the <c>sample data</c> block: which sample data the whole application previews with, chosen once here, or <c>null</c> for none.
    /// </summary>
    public SampleDataSelectionSyntax? SampleData { get; init; }
}

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
    string? Layout = null) : SyntaxNode(Location);

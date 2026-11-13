// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the kinds of visual a screen can carry beside its composed structure.
/// </summary>
public enum ScreenVisualKind
{
    /// <summary>
    /// A quick drawing of the screen, kept as vector drawing data - written <c>sketch &lt;path&gt;</c>.
    /// </summary>
    Sketch = 0,

    /// <summary>
    /// One HTML/CSS page of a coded mockup - written <c>mockup &lt;path&gt;</c>.
    /// </summary>
    Mockup = 1,

    /// <summary>
    /// A screenshot or a design export - written <c>image &lt;path&gt;</c>.
    /// </summary>
    Image = 2
}

/// <summary>
/// Represents a visual a screen carries - a sketch, an HTML mockup page or an image - stored beside the model.
/// </summary>
/// <remarks>
/// A visual is authoring metadata. It never changes what the screen does, so it does not reach the executable
/// semantic model; it is kept in the source, printed, exported and read by tools and builders.
/// </remarks>
/// <param name="Kind">The <see cref="ScreenVisualKind"/>.</param>
/// <param name="File">The <see cref="FileReferenceSyntax"/> pointing at the file, relative to the model root.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ScreenVisualSyntax(ScreenVisualKind Kind, FileReferenceSyntax File, SourceLocation Location) : SyntaxNode(Location);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>sticky note on &lt;canvas&gt;</c> declaration - a free-standing note placed on one of the domain canvases next to the
/// things it talks about: a goal, a pain point, a question. Authoring metadata, never part of the executable model.
/// </summary>
/// <param name="Canvas">The canvas the note is on: <c>personas</c>, <c>eventsources</c>, <c>givencontexts</c> or <c>seeddata</c>.</param>
/// <param name="Text">The text written on the note.</param>
/// <param name="Colour">The colour of the note, or <see langword="null"/> for the default one.</param>
/// <param name="X">Where the note sits on the canvas, horizontally.</param>
/// <param name="Y">Where the note sits on the canvas, vertically.</param>
/// <param name="Width">The width of the note, or <see langword="null"/> when the canvas decides.</param>
/// <param name="Height">The height of the note, or <see langword="null"/> when the canvas decides.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record StickyNoteSyntax(
    string Canvas,
    string Text,
    string? Colour,
    int X,
    int Y,
    int? Width,
    int? Height,
    SourceLocation Location) : SyntaxNode(Location);

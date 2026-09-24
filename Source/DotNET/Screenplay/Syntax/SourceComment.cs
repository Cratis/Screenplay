// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Identifies the canonical placement of an authored comment.
/// </summary>
public enum SourceCommentPlacement
{
    /// <summary>A full-line comment before the anchored line.</summary>
    Leading,

    /// <summary>A comment on the anchored line.</summary>
    Trailing,

    /// <summary>A full-line comment after the last member of the enclosing block.</summary>
    End
}

/// <summary>
/// A source comment attached to a syntax declaration, outside the typed syntax contract.
/// </summary>
/// <param name="Line">The original source line.</param>
/// <param name="Anchor">The significant source line beside or following the comment.</param>
/// <param name="Text">The comment, including its marker.</param>
/// <param name="Placement">Where to print the comment relative to its anchor.</param>
public sealed record SourceComment(int Line, string Anchor, string Text, SourceCommentPlacement Placement)
{
    /// <summary>
    /// Gets the source line that introduces the syntax owner of this comment.
    /// </summary>
    public string OwnerAnchor { get; init; } = string.Empty;
}

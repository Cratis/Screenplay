// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Represents a single line of source text, prepared for parsing.
/// </summary>
/// <param name="Number">The 1-based line number.</param>
/// <param name="Raw">The verbatim line text.</param>
/// <param name="Indent">The number of leading whitespace characters.</param>
/// <param name="Content">The line content without indentation, comments and trailing whitespace.</param>
/// <param name="Path">The path of the file the line came from, or <c>null</c> when the source text has no file identity.</param>
internal sealed record SourceLine(int Number, string Raw, int Indent, string Content, string? Path = null)
{
    /// <summary>
    /// Gets a value indicating whether the line has no significant content.
    /// </summary>
    public bool IsBlank => Content.Length == 0;

    /// <summary>
    /// Gets the <see cref="SourceLocation"/> of the first significant character on the line.
    /// </summary>
    public SourceLocation Location => new(Number, Indent + 1, Path);

    /// <summary>
    /// Gets the <see cref="SourceLocation"/> of the very start of the line, before any indentation.
    /// </summary>
    public SourceLocation Start => new(Number, 1, Path);

    /// <summary>
    /// Gets the <see cref="SourceLocation"/> at an offset into the line content.
    /// </summary>
    /// <param name="offset">The 0-based offset into the content.</param>
    /// <returns>The <see cref="SourceLocation"/> at the offset.</returns>
    public SourceLocation LocationAt(int offset) => new(Number, Indent + offset + 1, Path);
}

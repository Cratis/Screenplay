// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Represents a position in source text.
/// </summary>
/// <param name="Line">The 1-based line number.</param>
/// <param name="Column">The 1-based column number.</param>
/// <param name="Path">The path of the file the position belongs to, or <c>null</c> when the source text has no file identity.</param>
/// <remarks>
/// A single document needs no file identity - there is one source text, and the caller already knows which
/// file it handed over. A folder compiled as one application does: its diagnostics come from several files,
/// and a line and column alone cannot say which. Compiling a folder therefore parses every file with its
/// relative path, so the path travels with every location in the tree and every diagnostic pointing at one.
/// Compiling a single document passes no path, leaving <see cref="Path"/> null.
/// </remarks>
public record SourceLocation(int Line, int Column, string? Path = null)
{
    /// <summary>
    /// Gets the location representing the start of a document.
    /// </summary>
    public static readonly SourceLocation Start = new(1, 1);

    /// <summary>
    /// Gets the same position attributed to a file.
    /// </summary>
    /// <param name="path">The path to attribute the position to, or <c>null</c> to leave it unattributed.</param>
    /// <returns>The <see cref="SourceLocation"/> carrying the path.</returns>
    public SourceLocation In(string? path) => path is null ? this : this with { Path = path };
}

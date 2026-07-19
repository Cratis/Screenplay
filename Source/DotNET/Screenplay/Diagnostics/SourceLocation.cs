// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// Represents a position in source text.
/// </summary>
/// <param name="Line">The 1-based line number.</param>
/// <param name="Column">The 1-based column number.</param>
public record SourceLocation(int Line, int Column)
{
    /// <summary>
    /// Gets the location representing the start of a document.
    /// </summary>
    public static readonly SourceLocation Start = new(1, 1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents an exact range in one source document.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct SemanticSourceSpan
{
    SemanticSourceSpan(
        DocumentId document,
        int start,
        int length,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn)
    {
        Document = document;
        Start = start;
        Length = length;
        StartLine = startLine;
        StartColumn = startColumn;
        EndLine = endLine;
        EndColumn = endColumn;
    }

    /// <summary>
    /// Gets the source document identity.
    /// </summary>
    public DocumentId Document { get; }

    /// <summary>
    /// Gets the zero-based UTF-16 start offset.
    /// </summary>
    public int Start { get; }

    /// <summary>
    /// Gets the UTF-16 length.
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Gets the one-based start line.
    /// </summary>
    public int StartLine { get; }

    /// <summary>
    /// Gets the one-based start column.
    /// </summary>
    public int StartColumn { get; }

    /// <summary>
    /// Gets the one-based inclusive end line.
    /// </summary>
    public int EndLine { get; }

    /// <summary>
    /// Gets the one-based exclusive end column.
    /// </summary>
    public int EndColumn { get; }

    /// <summary>
    /// Creates a validated source span.
    /// </summary>
    /// <param name="document">The source document identity.</param>
    /// <param name="start">The zero-based UTF-16 start offset.</param>
    /// <param name="length">The UTF-16 length.</param>
    /// <param name="startLine">The one-based start line.</param>
    /// <param name="startColumn">The one-based start column.</param>
    /// <param name="endLine">The one-based inclusive end line.</param>
    /// <param name="endColumn">The one-based exclusive end column.</param>
    /// <returns>The validated source span.</returns>
    /// <exception cref="InvalidSemanticContract">The identity or range is invalid.</exception>
    public static SemanticSourceSpan Create(
        DocumentId document,
        int start,
        int length,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn)
    {
        if (!document.IsSet || start < 0 || length < 0 || startLine < 1 || startColumn < 1 || endLine < startLine || endColumn < 1 ||
            (endLine == startLine && endColumn < startColumn))
        {
            throw new InvalidSemanticContract("A semantic source span is malformed.");
        }

        return new(document, start, length, startLine, startColumn, endLine, endColumn);
    }
}

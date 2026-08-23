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
    /// Gets the one-based line containing the end-exclusive position.
    /// </summary>
    public int EndLine { get; }

    /// <summary>
    /// Gets the one-based column of the end-exclusive position.
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

    internal void ValidateAgainst(string text)
    {
        if (text is null || Length > text.Length || Start > text.Length - Length)
        {
            throw new InvalidSemanticContract("A semantic source span is outside its source document.");
        }

        var startPosition = PositionAt(text, Start);
        var endPosition = PositionAt(text, Start + Length);
        if (startPosition.Line != StartLine || startPosition.Column != StartColumn ||
            endPosition.Line != EndLine || endPosition.Column != EndColumn)
        {
            throw new InvalidSemanticContract("A semantic source span does not match its source document coordinates.");
        }
    }

    static (int Line, int Column) PositionAt(string text, int offset)
    {
        var line = 1;
        var column = 1;
        for (var index = 0; index < offset; index++)
        {
            if (text[index] == '\r')
            {
                if (index + 1 < text.Length && text[index + 1] == '\n')
                {
                    if (offset == index + 1)
                    {
                        throw new InvalidSemanticContract("A semantic source span cannot split a CRLF line ending.");
                    }

                    index++;
                }

                line++;
                column = 1;
            }
            else if (text[index] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }
}

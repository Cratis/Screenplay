// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Parsing;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Defines one lossless source-token role in a workspace document.
/// </summary>
public enum WorkspaceSourceTokenKind
{
    /// <summary>
    /// Leading whitespace before significant line content.
    /// </summary>
    Indentation = 0,

    /// <summary>
    /// Significant Screenplay source text.
    /// </summary>
    Text = 1,

    /// <summary>
    /// Trailing whitespace outside a comment.
    /// </summary>
    Whitespace = 2,

    /// <summary>
    /// A source comment and any text following its marker on the same line.
    /// </summary>
    Comment = 3,

    /// <summary>
    /// The exact parser line-ending sequence: <c>\n</c> and any immediately preceding <c>\r</c> characters.
    /// </summary>
    LineEnding = 4
}

/// <summary>
/// Identifies one exact source range in both decoded UTF-16 text and original UTF-8 bytes.
/// </summary>
public sealed record WorkspaceSourceSpan
{
    /// <summary>
    /// Gets the zero-based UTF-16 offset in <see cref="WorkspaceDocument.Text"/>.
    /// </summary>
    public required int TextOffset { get; init; }

    /// <summary>
    /// Gets the UTF-16 length.
    /// </summary>
    public required int TextLength { get; init; }

    /// <summary>
    /// Gets the zero-based byte offset in <see cref="WorkspaceDocument.Bytes"/>, including any UTF-8 BOM.
    /// </summary>
    public required int ByteOffset { get; init; }

    /// <summary>
    /// Gets the byte length.
    /// </summary>
    public required int ByteLength { get; init; }

    /// <summary>
    /// Gets the 1-based source line.
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets the 1-based UTF-16 source column.
    /// </summary>
    public required int Column { get; init; }
}

/// <summary>
/// Represents one exact lossless token from a workspace document.
/// </summary>
public sealed record WorkspaceSourceToken
{
    /// <summary>
    /// Gets the token role.
    /// </summary>
    public required WorkspaceSourceTokenKind Kind { get; init; }

    /// <summary>
    /// Gets the exact decoded token text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Gets the exact text and byte range.
    /// </summary>
    public required WorkspaceSourceSpan Span { get; init; }
}

/// <summary>
/// Represents one immutable workspace document and its lossless token stream.
/// </summary>
public sealed record WorkspaceTokenDocument
{
    /// <summary>
    /// Gets the exact source document.
    /// </summary>
    public required WorkspaceDocument Document { get; init; }

    /// <summary>
    /// Gets tokens in exact source order.
    /// </summary>
    public ImmutableArray<WorkspaceSourceToken> Tokens { get; init; } = [];
}

/// <summary>
/// Produces lossless token evidence without changing parser behavior.
/// </summary>
public static class WorkspaceSourceTokenizer
{
    static readonly UTF8Encoding _utf8 = new(false, true);

    /// <summary>
    /// Tokenizes an exact workspace document while preserving every source byte after an optional BOM.
    /// </summary>
    /// <param name="document">The document to tokenize.</param>
    /// <returns>The immutable token document.</returns>
    public static WorkspaceTokenDocument Tokenize(WorkspaceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var tokens = ImmutableArray.CreateBuilder<WorkspaceSourceToken>();
        var text = document.Text;
        var textOffset = 0;
        var byteOffset = document.Encoding == WorkspaceTextEncoding.Utf8WithBom
            ? Encoding.UTF8.Preamble.Length
            : 0;
        var line = 1;

        for (; textOffset < text.Length; line++)
        {
            var lineStart = textOffset;
            while (textOffset < text.Length && text[textOffset] != '\n')
            {
                textOffset++;
            }

            var contentEnd = textOffset;
            if (textOffset < text.Length)
            {
                while (contentEnd > lineStart && text[contentEnd - 1] == '\r')
                {
                    contentEnd--;
                }
            }

            AddLineTokens(tokens, text, lineStart, contentEnd, byteOffset, line);
            byteOffset += _utf8.GetByteCount(text.AsSpan(lineStart, contentEnd - lineStart));

            if (textOffset < text.Length)
            {
                var endingLength = textOffset - contentEnd + 1;
                AddToken(tokens, WorkspaceSourceTokenKind.LineEnding, text, contentEnd, endingLength, byteOffset, line, contentEnd - lineStart + 1);
                byteOffset += _utf8.GetByteCount(text.AsSpan(contentEnd, endingLength));
                textOffset++;
            }
        }

        return new WorkspaceTokenDocument
        {
            Document = document,
            Tokens = tokens.ToImmutable()
        };
    }

    static void AddLineTokens(
        ImmutableArray<WorkspaceSourceToken>.Builder tokens,
        string text,
        int lineStart,
        int lineEnd,
        int lineByteOffset,
        int line)
    {
        var lineText = text.AsSpan(lineStart, lineEnd - lineStart);
        var indentationLength = 0;
        while (indentationLength < lineText.Length && char.IsWhiteSpace(lineText[indentationLength]))
        {
            indentationLength++;
        }

        var byteOffset = lineByteOffset;
        if (indentationLength > 0)
        {
            AddToken(tokens, WorkspaceSourceTokenKind.Indentation, text, lineStart, indentationLength, byteOffset, line, 1);
            byteOffset += _utf8.GetByteCount(lineText[..indentationLength]);
        }

        var body = lineText[indentationLength..];

        // Portable .play documents use the compiler's default slash-comment grammar; hash comments are only admitted by embedded fragment compilers.
        var commentStart = SourceLineSplitter.CommentStart(body.ToString());
        var beforeComment = commentStart < 0 ? body : body[..commentStart];
        var textLength = beforeComment.Length;
        while (textLength > 0 && char.IsWhiteSpace(beforeComment[textLength - 1]))
        {
            textLength--;
        }

        if (textLength > 0)
        {
            AddToken(tokens, WorkspaceSourceTokenKind.Text, text, lineStart + indentationLength, textLength, byteOffset, line, indentationLength + 1);
            byteOffset += _utf8.GetByteCount(beforeComment[..textLength]);
        }

        var whitespaceLength = beforeComment.Length - textLength;
        if (whitespaceLength > 0)
        {
            AddToken(tokens, WorkspaceSourceTokenKind.Whitespace, text, lineStart + indentationLength + textLength, whitespaceLength, byteOffset, line, indentationLength + textLength + 1);
            byteOffset += _utf8.GetByteCount(beforeComment[textLength..]);
        }

        if (commentStart >= 0)
        {
            var commentLength = body.Length - commentStart;
            AddToken(tokens, WorkspaceSourceTokenKind.Comment, text, lineStart + indentationLength + commentStart, commentLength, byteOffset, line, indentationLength + commentStart + 1);
        }
    }

    static void AddToken(
        ImmutableArray<WorkspaceSourceToken>.Builder tokens,
        WorkspaceSourceTokenKind kind,
        string source,
        int textOffset,
        int textLength,
        int byteOffset,
        int line,
        int column)
    {
        var text = source.Substring(textOffset, textLength);
        tokens.Add(new WorkspaceSourceToken
        {
            Kind = kind,
            Text = text,
            Span = new WorkspaceSourceSpan
            {
                TextOffset = textOffset,
                TextLength = textLength,
                ByteOffset = byteOffset,
                ByteLength = _utf8.GetByteCount(text),
                Line = line,
                Column = column
            }
        });
    }
}

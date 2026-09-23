// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Resolves parser-owned single-line source spans to exact UTF-8 byte ranges of a workspace document.
/// </summary>
static class WorkspaceSourceRanges
{
    /// <summary>
    /// Resolves a span that lies entirely inside one significant text token.
    /// </summary>
    /// <param name="tokens">The document's lossless tokens.</param>
    /// <param name="start">The parser-owned start of the span.</param>
    /// <param name="length">The UTF-16 length of the span.</param>
    /// <returns>The byte offset and length, or <c>null</c> when the span is not inside exactly one text token.</returns>
    internal static (int Offset, int Length)? Bytes(ImmutableArray<WorkspaceSourceToken> tokens, SourceLocation start, int length)
    {
        var candidates = tokens.Where(token => token.Kind == WorkspaceSourceTokenKind.Text && token.Span.Line == start.Line &&
            token.Span.Column <= start.Column && start.Column - token.Span.Column + length <= token.Span.TextLength).ToArray();
        if (candidates.Length != 1 || length < 0)
        {
            return null;
        }

        var token = candidates[0];
        var offset = start.Column - token.Span.Column;
        return (
            token.Span.ByteOffset + Encoding.UTF8.GetByteCount(token.Text.AsSpan(0, offset)),
            Encoding.UTF8.GetByteCount(token.Text.AsSpan(offset, length)));
    }
}

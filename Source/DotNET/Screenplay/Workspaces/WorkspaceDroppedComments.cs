// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Represents one authored comment that a changed document no longer contains.
/// </summary>
public sealed record WorkspaceDroppedComment
{
    /// <summary>
    /// Gets the changed document.
    /// </summary>
    public required DocumentId Document { get; init; }

    /// <summary>
    /// Gets the document's original path.
    /// </summary>
    public required PortablePlayPath Path { get; init; }

    /// <summary>
    /// Gets the 1-based line of the comment in the original document.
    /// </summary>
    public required int Line { get; init; }

    /// <summary>
    /// Gets the 1-based UTF-16 column of the comment in the original document.
    /// </summary>
    public required int Column { get; init; }

    /// <summary>
    /// Gets the exact comment text, including its marker.
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// Finds authored comments that a proposed change drops, so a reviewer can see them before applying it.
/// </summary>
/// <remarks>
/// Comments are compared by exact text, as a multiset: a comment that survives anywhere in the changed document,
/// even on another line, is not reported. A document created, removed, or unchanged by a plan drops nothing.
/// </remarks>
public static class WorkspaceDroppedComments
{
    /// <summary>
    /// Finds every comment dropped by the documents a write plan changes in place.
    /// </summary>
    /// <param name="plan">The <see cref="WorkspaceWritePlan"/> to review.</param>
    /// <returns>The dropped comments, in document and source order.</returns>
    public static ImmutableArray<WorkspaceDroppedComment> In(WorkspaceWritePlan plan) =>
        [.. plan.Entries.Where(entry => entry.Before is not null && entry.After is not null).SelectMany(entry => Between(entry.Before!, entry.After!))];

    /// <summary>
    /// Finds every comment of one document that its replacement no longer contains.
    /// </summary>
    /// <param name="before">The original document.</param>
    /// <param name="after">The changed document.</param>
    /// <returns>The dropped comments, in source order.</returns>
    public static ImmutableArray<WorkspaceDroppedComment> Between(WorkspaceDocument before, WorkspaceDocument after)
    {
        if (before.Bytes.AsSpan().SequenceEqual(after.Bytes.AsSpan()))
        {
            return [];
        }

        var remaining = Comments(after).GroupBy(comment => comment.Text, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var dropped = ImmutableArray.CreateBuilder<WorkspaceDroppedComment>();
        foreach (var comment in Comments(before))
        {
            if (remaining.TryGetValue(comment.Text, out var count) && count > 0)
            {
                remaining[comment.Text] = count - 1;
                continue;
            }

            dropped.Add(new()
            {
                Document = before.Id,
                Path = before.Path,
                Line = comment.Span.Line,
                Column = comment.Span.Column,
                Text = comment.Text
            });
        }

        return dropped.ToImmutable();
    }

    static IEnumerable<WorkspaceSourceToken> Comments(WorkspaceDocument document) =>
        WorkspaceSourceTokenizer.Tokenize(document).Tokens.Where(token => token.Kind == WorkspaceSourceTokenKind.Comment);
}

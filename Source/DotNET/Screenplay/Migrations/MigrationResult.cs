// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Represents what one migration step did to one document.
/// </summary>
/// <param name="Document">The <see cref="DocumentSyntax"/> with the rewritten tree. Its source is still the text the step read.</param>
/// <param name="Changes">The <see cref="MigrationChange">changes</see>, in source order.</param>
/// <param name="Reviews">What the step could not decide alone and left for the author, as <see cref="MigrationReview">reviews</see>.</param>
public sealed record MigrationResult(DocumentSyntax Document, IReadOnlyList<MigrationChange> Changes, IReadOnlyList<MigrationReview> Reviews)
{
    /// <summary>
    /// Creates a result for a document the step had nothing to change in.
    /// </summary>
    /// <param name="document">The document.</param>
    /// <returns>The unchanged <see cref="MigrationResult"/>.</returns>
    public static MigrationResult Unchanged(DocumentSyntax document) => new(document, [], []);
}

/// <summary>
/// Represents one change a step made, as it reads in a report, with the source edits that write it.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> of the retired form.</param>
/// <param name="Before">The line as it was written.</param>
/// <param name="After">The line as it is written now.</param>
/// <param name="Edits">The <see cref="MigrationEdit">edits</see> to the source lines that write the change.</param>
public sealed record MigrationChange(SourceLocation Location, string Before, string After, IReadOnlyList<MigrationEdit> Edits);

/// <summary>
/// Represents something a step could not decide alone - an author has to look at it.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> to look at.</param>
/// <param name="Message">What to look at, and what the step did meanwhile.</param>
public sealed record MigrationReview(SourceLocation Location, string Message);

/// <summary>
/// Represents an edit to the source lines a step read, written by printing nodes of the rewritten tree. Line numbers are
/// 1-based and refer to the text the step read; a block is a line and every deeper indented line beneath it.
/// </summary>
/// <param name="Line">The line the edit starts at.</param>
public abstract record MigrationEdit(int Line);

/// <summary>
/// Replaces one line with the first printed line of each node, at the line's indentation. A comment at the end of the
/// line is kept on the first replacement; the lines beneath it are not touched.
/// </summary>
/// <param name="Line">The line to replace.</param>
/// <param name="Nodes">The nodes whose first printed lines replace it.</param>
public sealed record ReplaceLine(int Line, IReadOnlyList<SyntaxNode> Nodes) : MigrationEdit(Line);

/// <summary>
/// Inserts a printed node after the block that starts at a line, at that line's indentation.
/// </summary>
/// <param name="Line">The line whose block the node goes after.</param>
/// <param name="Node">The node to print.</param>
public sealed record InsertAfterBlock(int Line, SyntaxNode Node) : MigrationEdit(Line);

/// <summary>
/// Moves the block at a line, with the comment lines directly above it, to the end of another block. The first line is
/// written as the first printed line of a node; the lines beneath it move unchanged, re-indented to their new parent.
/// </summary>
/// <param name="Line">The first line of the block to move.</param>
/// <param name="After">The line whose block it moves to the end of.</param>
/// <param name="Node">The node whose first printed line replaces the block's first line.</param>
public sealed record MoveBlock(int Line, int After, SyntaxNode Node) : MigrationEdit(Line);

/// <summary>
/// Removes one line, leaving the lines beneath it.
/// </summary>
/// <param name="Line">The line to remove.</param>
public sealed record RemoveLine(int Line) : MigrationEdit(Line);

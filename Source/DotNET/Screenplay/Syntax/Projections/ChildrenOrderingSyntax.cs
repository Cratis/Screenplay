// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// Represents the ordering line of a <c>children</c> block: <c>insert first</c>, <c>insert last</c> or
/// <c>sort by &lt;property&gt; [ascending|descending]</c>.
/// </summary>
/// <param name="Placement">The <see cref="ChildrenPlacement"/> a new child gets.</param>
/// <param name="SortBy">The dotted child property path the collection is sorted by, when <paramref name="Placement"/> is <see cref="ChildrenPlacement.Sorted"/>; otherwise <see langword="null"/>.</param>
/// <param name="Direction">The <see cref="ChildrenSortDirection"/> of a sorted collection; <see cref="ChildrenSortDirection.Ascending"/> otherwise.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// The order is stored, not computed when the read model is read: a new child is placed when it is added, and an update
/// to an existing child - including a change to its sort property - never moves it. Sorting is stable, with children
/// that have no value first when ascending and last when descending.
/// </remarks>
public record ChildrenOrderingSyntax(
    ChildrenPlacement Placement,
    string? SortBy,
    ChildrenSortDirection Direction,
    SourceLocation Location) : SyntaxNode(Location);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// Defines where a <c>children</c> block puts a child it adds to its collection.
/// </summary>
/// <remarks>
/// The order is stored: it is applied when a child is added, and an update to an existing child never moves it.
/// </remarks>
public enum ChildrenPlacement
{
    /// <summary>
    /// A new child is added at the end of the collection - <c>insert last</c>, and what a block without an ordering does.
    /// </summary>
    Append = 0,

    /// <summary>
    /// A new child is added at the start of the collection - <c>insert first</c>.
    /// </summary>
    Prepend = 1,

    /// <summary>
    /// The collection is ordered by a child property after a new child is added - <c>sort by &lt;property&gt;</c>.
    /// </summary>
    Sorted = 2
}

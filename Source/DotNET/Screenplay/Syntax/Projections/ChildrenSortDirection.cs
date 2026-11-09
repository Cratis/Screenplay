// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// Defines the direction a <c>sort by</c> children ordering sorts in.
/// </summary>
public enum ChildrenSortDirection
{
    /// <summary>
    /// Smallest first; a child without a value sorts first. What <c>sort by</c> does without a direction.
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// Largest first; a child without a value sorts last.
    /// </summary>
    Descending = 1
}

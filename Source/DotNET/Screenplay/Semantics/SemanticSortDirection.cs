// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the direction a sorted children collection is ordered in.
/// </summary>
public enum SemanticSortDirection
{
    /// <summary>
    /// The direction is not one this version of the model knows.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Smallest first; a child without a value first.
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// Largest first; a child without a value last.
    /// </summary>
    Descending = 1
}

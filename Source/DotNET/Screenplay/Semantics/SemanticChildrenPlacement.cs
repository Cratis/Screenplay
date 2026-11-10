// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines where a children collection places a new child.
/// </summary>
public enum SemanticChildrenPlacement
{
    /// <summary>
    /// The placement is not one this version of the model knows.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A new child is added at the end.
    /// </summary>
    Append = 0,

    /// <summary>
    /// A new child is added at the start.
    /// </summary>
    Prepend = 1,

    /// <summary>
    /// The collection is ordered by an element property after a new child is added.
    /// </summary>
    Sorted = 2
}

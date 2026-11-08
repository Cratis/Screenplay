// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines how a slice changed between two versions of a model.
/// </summary>
public enum SliceChangeKind
{
    /// <summary>
    /// The slice exists only after the comparison.
    /// </summary>
    Added,

    /// <summary>
    /// The slice exists only before the comparison.
    /// </summary>
    Removed,

    /// <summary>
    /// The slice keeps its identity under a new name.
    /// </summary>
    Renamed,

    /// <summary>
    /// The slice keeps its identity under another feature or module, or in another document.
    /// </summary>
    Moved,

    /// <summary>
    /// A declaration owned by the slice was added, removed, renamed, moved or changed.
    /// </summary>
    Changed
}

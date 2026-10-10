// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines DeclarationChangeKind values for structural model comparison.
/// </summary>
public enum DeclarationChangeKind
{
    /// <summary>
    /// The declaration exists only after the comparison.
    /// </summary>
    Added,

    /// <summary>
    /// The declaration exists only before the comparison.
    /// </summary>
    Removed,

    /// <summary>
    /// A persisted declaration keeps its identity under a new name.
    /// </summary>
    Renamed,

    /// <summary>
    /// A persisted declaration changes owner or document.
    /// </summary>
    Moved
}

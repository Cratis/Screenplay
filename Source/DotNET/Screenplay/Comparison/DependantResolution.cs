// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines DependantResolution values for structural model comparison.
/// </summary>
public enum DependantResolution
{
    /// <summary>
    /// One target of the expected kind is resolved.
    /// </summary>
    Resolved,

    /// <summary>
    /// No indexed target is found.
    /// </summary>
    Unresolved,

    /// <summary>
    /// Multiple targets or source owners are possible.
    /// </summary>
    Ambiguous,

    /// <summary>
    /// Source ownership is incomplete.
    /// </summary>
    Incomplete,

    /// <summary>
    /// The indexed target has an unexpected kind.
    /// </summary>
    WrongKind
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Identifies the construct a standalone concept finding concerns, never a source location.
/// </summary>
public enum SemanticConceptFindingTarget
{
    /// <summary>
    /// No construct has been established.
    /// </summary>
    Unknown,

    /// <summary>
    /// The generation declaration as a whole.
    /// </summary>
    Generation,

    /// <summary>
    /// One named generation option.
    /// </summary>
    Option,

    /// <summary>
    /// A validation rule in flattened declaration order.
    /// </summary>
    Rule
}

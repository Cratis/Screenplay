// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Identifies the smallest executable-binding unit excluded from an application.
/// </summary>
public enum SemanticExclusionKind
{
    /// <summary>
    /// A specification, retaining its slice.
    /// </summary>
    Specification,

    /// <summary>
    /// A slice and its contained declarations.
    /// </summary>
    Slice,

    /// <summary>
    /// An application-level import.
    /// </summary>
    Import,

    /// <summary>
    /// An application-level policy.
    /// </summary>
    Policy
}

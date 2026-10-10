// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines MemberChangeKind values for structural model comparison.
/// </summary>
public enum MemberChangeKind
{
    /// <summary>
    /// An authored member changes.
    /// </summary>
    Changed,

    /// <summary>
    /// Opaque inline content or a file reference changes.
    /// </summary>
    OpaqueChanged
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines SpecificationChangeKind values for structural model comparison.
/// </summary>
public enum SpecificationChangeKind
{
    /// <summary>
    /// A specification is added.
    /// </summary>
    Added,

    /// <summary>
    /// A specification is removed.
    /// </summary>
    Removed,

    /// <summary>
    /// An effective expected outcome changes.
    /// </summary>
    ExpectedOutcomeChanged,

    /// <summary>
    /// Opaque content in an expected outcome changes.
    /// </summary>
    OpaqueChanged
}

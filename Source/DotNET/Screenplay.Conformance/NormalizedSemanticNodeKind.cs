// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Describes the kind of a normalised semantic node.
/// </summary>
internal enum NormalizedSemanticNodeKind
{
    /// <summary>
    /// A JSON object; its members are its properties.
    /// </summary>
    Object = 0,

    /// <summary>
    /// An unordered array; its members are its elements, keyed by segment.
    /// </summary>
    Set = 1,

    /// <summary>
    /// An ordered array; its items are its elements, by position.
    /// </summary>
    List = 2,

    /// <summary>
    /// A scalar, held as compact canonical JSON text.
    /// </summary>
    Leaf = 3,
}

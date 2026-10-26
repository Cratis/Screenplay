// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Describes how the canonical writer orders an array, and therefore how the normaliser compares it.
/// </summary>
internal enum SemanticArrayKind
{
    /// <summary>
    /// The writer sorts the elements by their identity; they are compared as a set keyed by structural key.
    /// </summary>
    IdentitySet = 0,

    /// <summary>
    /// The writer sorts the elements by name; they are compared as a set keyed by name.
    /// </summary>
    NameSet = 1,

    /// <summary>
    /// The writer sorts the elements by an identity they reference; they are re-sorted after references are normalised.
    /// </summary>
    ReferenceSet = 2,

    /// <summary>
    /// The writer keeps authored order, which is semantic; elements are compared by position.
    /// </summary>
    Ordered = 3,
}

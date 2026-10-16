// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes an option's source value, independently of a rendering target.
/// </summary>
public enum GenerationOptionKind
{
    /// <summary>
    /// No value kind has been established.
    /// </summary>
    Unknown,

    /// <summary>
    /// A quoted string literal.
    /// </summary>
    Text,

    /// <summary>
    /// An integer argument.
    /// </summary>
    WholeNumber,

    /// <summary>
    /// One of the listed keyword choices.
    /// </summary>
    Choice,

    /// <summary>
    /// Ordered inclusive Int64 endpoints separated by two dots.
    /// </summary>
    Int64Range,

    /// <summary>
    /// A quoted finite generation pattern, not a match regular expression.
    /// </summary>
    Pattern,

    /// <summary>
    /// An identifier naming a generation rule.
    /// </summary>
    Name,

    /// <summary>
    /// Exactly one file reference or tagged inline implementation.
    /// </summary>
    Implementation
}

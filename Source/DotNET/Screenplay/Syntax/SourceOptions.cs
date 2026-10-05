// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the interpretation of numeric literals, independently of executable feature versions.
/// </summary>
public enum NumericMode
{
    /// <summary>
    /// Retain the existing Double source interpretation.
    /// </summary>
    Legacy,

    /// <summary>
    /// Read explicitly opted-in literals in the bounded exact Decimal domain.
    /// </summary>
    Exact
}

/// <summary>
/// Represents immutable physical-document source options. Absence of a preamble selects Legacy.
/// </summary>
/// <param name="NumericMode">The explicitly selected numeric interpretation.</param>
public sealed record SourceOptions(NumericMode NumericMode)
{
    /// <summary>
    /// Gets the options for an unmarked document.
    /// </summary>
    public static SourceOptions Legacy { get; } = new(NumericMode.Legacy);

    /// <summary>
    /// Gets the options selected by <c>numbers exact</c>.
    /// </summary>
    public static SourceOptions Exact { get; } = new(NumericMode.Exact);
}

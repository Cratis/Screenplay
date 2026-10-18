// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines how an authored enumeration assigns numeric identities.
/// </summary>
public enum EnumerationNumbering
{
    /// <summary>
    /// Numbers are zero-based declaration positions.
    /// </summary>
    Positional,

    /// <summary>
    /// Every member declares its number explicitly, including pins equal to positions.
    /// </summary>
    Explicit
}

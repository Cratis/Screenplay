// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Classifies an authored match pattern without executing it.
/// </summary>
public enum PortableMatchStatus
{
    /// <summary>
    /// The pattern belongs to the portable dialect.
    /// </summary>
    Admitted,

    /// <summary>
    /// The pattern is a deprecated Studio text comparison.
    /// </summary>
    Deprecated,

    /// <summary>
    /// The pattern uses a construct outside the portable dialect.
    /// </summary>
    Refused,

    /// <summary>
    /// The pattern has invalid syntax.
    /// </summary>
    Invalid
}

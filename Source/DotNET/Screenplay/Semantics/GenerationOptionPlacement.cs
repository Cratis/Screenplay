// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes where an option appears in a generation declaration.
/// </summary>
public enum GenerationOptionPlacement
{
    /// <summary>
    /// No placement has been established.
    /// </summary>
    Unknown,

    /// <summary>
    /// The option is a positional argument, in catalog order.
    /// </summary>
    Argument,

    /// <summary>
    /// The option follows its keyword.
    /// </summary>
    Modifier,

    /// <summary>
    /// The option is a file or tagged inline body beneath the declaration.
    /// </summary>
    Body
}

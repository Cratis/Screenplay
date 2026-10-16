// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes the closed vocabulary of option applicability conditions.
/// </summary>
public enum GenerationOptionConditionKind
{
    /// <summary>
    /// An unknown condition must not be treated as satisfied.
    /// </summary>
    Unknown,

    /// <summary>
    /// The concept's source primitive must be one of the listed names.
    /// </summary>
    PrimitiveIs,

    /// <summary>
    /// Another option must have one of the listed values.
    /// </summary>
    OptionIn
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// A value-free reason why a portable stream identity cannot be formatted.
/// </summary>
public enum StreamIdFormatFailure
{
    /// <summary>
    /// No failure.
    /// </summary>
    None,

    /// <summary>
    /// Empty text.
    /// </summary>
    Empty,

    /// <summary>
    /// Text is not Unicode NFC.
    /// </summary>
    NotNfc,

    /// <summary>
    /// Text contains a lone UTF-16 surrogate.
    /// </summary>
    LoneSurrogate,

    /// <summary>
    /// An integer exceeds the Double numeric mode bound.
    /// </summary>
    OutOfRange,

    /// <summary>
    /// A numeric value is not a finite integer.
    /// </summary>
    NotIntegral,

    /// <summary>
    /// A UUID is not in an authored portable form.
    /// </summary>
    MalformedUuid,

    /// <summary>
    /// The number of components does not match the declaration.
    /// </summary>
    Arity,

    /// <summary>
    /// An escape is not %25 or %7C.
    /// </summary>
    Escape,

    /// <summary>
    /// A decoded component is not canonically spelled.
    /// </summary>
    Noncanonical
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// The portable scalar kinds in a declared stream identity.
/// </summary>
public enum StreamIdScalarKind
{
    /// <summary>
    /// Unicode NFC text.
    /// </summary>
    Text,

    /// <summary>
    /// A UUID in lowercase D form.
    /// </summary>
    Uuid,

    /// <summary>
    /// An integer in invariant decimal form.
    /// </summary>
    WholeNumber
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines what an <c>$eventContext.&lt;path&gt;</c> read resolves to in ESM v1.
/// </summary>
enum SemanticEventContextScalarKind
{
    /// <summary>
    /// The path is not admitted.
    /// </summary>
    Rejected = 0,

    /// <summary>
    /// The path reads one portable scalar value from the event context.
    /// </summary>
    Scalar = 1,

    /// <summary>
    /// The path reads the event source identity.
    /// </summary>
    EventSource = 2
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Why an element is not offered to a caller.
/// </summary>
public enum SemanticUnavailableCause
{
    /// <summary>The element is offered, or the model cannot say.</summary>
    None = 0,

    /// <summary>The caller does not satisfy the authorization of the element.</summary>
    NotAuthorized = 1,

    /// <summary>The part the element binds, or a gate on the element, is not in the caller's offer.</summary>
    NotAvailable = 2,

    /// <summary>An operator has suspended the part the element binds, or something above it.</summary>
    Suspended = 3
}

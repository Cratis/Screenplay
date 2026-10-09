// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines whether an event is an internal fact or an application's public contract.
/// </summary>
public enum EventVisibility
{
    /// <summary>
    /// A private, local fact; the default for unmarked events and ordinary imports.
    /// </summary>
    Private = 0,

    /// <summary>
    /// A public contract, owned locally or by the opaque origin that declares it.
    /// </summary>
    Public = 1
}

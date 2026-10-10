// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines IdentityChangeKind values for structural model comparison.
/// </summary>
public enum IdentityChangeKind
{
    /// <summary>
    /// A semantic or event-contract identity is assigned.
    /// </summary>
    Assigned,

    /// <summary>
    /// A semantic or event-contract identity is retired.
    /// </summary>
    Retired,

    /// <summary>
    /// An identity is preserved at a changed address.
    /// </summary>
    Migrated
}

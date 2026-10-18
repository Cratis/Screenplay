// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines changes to baseline enumeration members' numeric identities.
/// </summary>
public enum EnumerationMemberChangeKind
{
    /// <summary>
    /// A surviving name has a different effective number.
    /// </summary>
    NumberChanged,

    /// <summary>
    /// A name and its number are absent from the candidate.
    /// </summary>
    Removed,

    /// <summary>
    /// A removed name's number is held by another member without explicit rename evidence.
    /// </summary>
    NumberReused,

    /// <summary>
    /// An explicitly numbered member has a new name with the same explicit number.
    /// </summary>
    Renamed
}

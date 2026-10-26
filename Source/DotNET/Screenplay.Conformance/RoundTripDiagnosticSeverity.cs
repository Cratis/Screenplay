// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Describes the severity of a round-trip diagnostic.
/// </summary>
internal enum RoundTripDiagnosticSeverity
{
    /// <summary>
    /// Information; it never needs a ledger entry.
    /// </summary>
    Information = 0,

    /// <summary>
    /// A warning; it must be cited by a ledger entry.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// An error; it must be cited by a ledger entry.
    /// </summary>
    Error = 2,
}

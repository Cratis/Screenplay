// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Defines DeclarationMatching values for structural model comparison.
/// </summary>
public enum DeclarationMatching
{
    /// <summary>
    /// Matches persisted semantic identities within one application.
    /// </summary>
    Identity,

    /// <summary>
    /// Matches exact kinds and addresses, without inferring identity continuity.
    /// </summary>
    Address
}

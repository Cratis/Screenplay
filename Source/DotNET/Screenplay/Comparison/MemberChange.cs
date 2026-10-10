// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Describes an authored member difference, not executable equivalence.
/// </summary>
/// <param name="Declaration">The changed declaration.</param>
/// <param name="Member">The authored member name.</param>
/// <param name="Change">The member change.</param>
/// <param name="BeforeHash">The baseline SHA-256 hash, or null when the member is absent.</param>
/// <param name="AfterHash">The candidate SHA-256 hash, or null when the member is absent.</param>
/// <param name="ContractBreaking">Whether the change breaks the stream key contract.</param>
public sealed record MemberChange(
    ComparedDeclaration Declaration,
    string Member,
    MemberChangeKind Change,
    string? BeforeHash,
    string? AfterHash,
    bool ContractBreaking);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Reports a baseline enumeration member whose stored numeric identity changed or acquired a new name.
/// </summary>
/// <param name="Declaration">The matched enumeration concept.</param>
/// <param name="Change">The numeric identity change.</param>
/// <param name="Member">The baseline member name.</param>
/// <param name="AfterMember">The surviving member or candidate holder of its number; null for removal.</param>
/// <param name="BeforeNumber">The baseline effective number.</param>
/// <param name="AfterNumber">The candidate effective number; null for removal.</param>
/// <param name="BeforeNumbering">The baseline authored numbering mode.</param>
/// <param name="AfterNumbering">The candidate authored numbering mode.</param>
/// <param name="ContractBreaking">Whether the kind changes numeric identity, independently of storage usage.</param>
/// <param name="Stored">Whether the baseline enumeration is reachable from an event property, including through composite types.</param>
public sealed record EnumerationMemberChange(
    ComparedDeclaration Declaration,
    EnumerationMemberChangeKind Change,
    string Member,
    string? AfterMember,
    int BeforeNumber,
    int? AfterNumber,
    EnumerationNumbering BeforeNumbering,
    EnumerationNumbering AfterNumbering,
    bool ContractBreaking,
    bool Stored);

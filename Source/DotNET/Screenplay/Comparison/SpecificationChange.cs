// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Describes specification presence and effective expected outcome changes.
/// </summary>
/// <param name="Declaration">The changed specification.</param>
/// <param name="Change">The specification change.</param>
/// <param name="Member">The expected-outcome member, if applicable.</param>
/// <param name="BeforeHash">The baseline member hash, or null if absent.</param>
/// <param name="AfterHash">The candidate member hash, or null if absent.</param>
public sealed record SpecificationChange(
    ComparedDeclaration Declaration,
    SpecificationChangeKind Change,
    string? Member,
    string? BeforeHash,
    string? AfterHash);

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Describes an event shape or generation change while keeping contract risk visible.
/// </summary>
/// <param name="Declaration">The changed event.</param>
/// <param name="Change">The contract change.</param>
/// <param name="Property">The property name for a shape change.</param>
/// <param name="BeforeType">The baseline serialized syntax type, or null if absent.</param>
/// <param name="AfterType">The candidate serialized syntax type, or null if absent.</param>
/// <param name="ContractBreaking">Whether existing consumers face contract risk.</param>
/// <param name="GenerationCovered">Whether a marked generation preserves the previous shape.</param>
/// <param name="BeforeGeneration">The preceding generation, when applicable.</param>
/// <param name="AfterGeneration">The candidate generation, when applicable.</param>
public sealed record EventContractChange(
    ComparedDeclaration Declaration,
    EventContractChangeKind Change,
    string? Property,
    string? BeforeType,
    string? AfterType,
    bool ContractBreaking,
    bool GenerationCovered,
    uint? BeforeGeneration,
    uint? AfterGeneration);

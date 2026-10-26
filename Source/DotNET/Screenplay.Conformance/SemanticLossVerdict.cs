// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System.Collections.Immutable;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents the result of reconciling round-trip observations against a loss ledger.
/// </summary>
internal sealed record SemanticLossVerdict
{
    /// <summary>
    /// Gets the number of fixtures observed.
    /// </summary>
    public int Executed { get; init; }

    /// <summary>
    /// Gets the number of observations whose recovered source compiled and was compared.
    /// </summary>
    public int Compared { get; init; }

    /// <summary>
    /// Gets the problems with the ledger itself and the shape of its entries (R5).
    /// </summary>
    public ImmutableArray<SemanticLossFinding> Malformed { get; init; } = [];

    /// <summary>
    /// Gets the differences and failed recoveries no entry accepts (R1).
    /// </summary>
    public ImmutableArray<SemanticLossFinding> Unledgered { get; init; } = [];

    /// <summary>
    /// Gets the entries for observed fixtures that match nothing (R2).
    /// </summary>
    public ImmutableArray<SemanticLossFinding> Stale { get; init; } = [];

    /// <summary>
    /// Gets the entries citing codes that did not occur, and silent losses accepted by design (R3).
    /// </summary>
    public ImmutableArray<SemanticLossFinding> Unexplained { get; init; } = [];

    /// <summary>
    /// Gets the warnings and errors no entry cites (R4).
    /// </summary>
    public ImmutableArray<SemanticLossFinding> Uncited { get; init; } = [];

    /// <summary>
    /// Gets the budgets the ledger exceeds (R6).
    /// </summary>
    public ImmutableArray<SemanticLossFinding> OverBudget { get; init; } = [];

    /// <summary>
    /// Gets the reasons the run proves nothing (R7).
    /// </summary>
    public ImmutableArray<SemanticLossFinding> Vacuous { get; init; } = [];

    /// <summary>
    /// Gets the entries whose fixture a complete run neither observed nor may observe (R7).
    /// </summary>
    public ImmutableArray<SemanticLossFinding> Orphans { get; init; } = [];

    /// <summary>
    /// Gets the exclusions naming a fixture that is not eligible.
    /// </summary>
    public ImmutableArray<SemanticLossFinding> IneligibleExclusions { get; init; } = [];

    /// <summary>
    /// Gets the exclusions naming a fixture that was observed after all.
    /// </summary>
    public ImmutableArray<SemanticLossFinding> ObsoleteExclusions { get; init; } = [];

    /// <summary>
    /// Gets the eligible fixtures a complete run neither observed nor excluded (R7).
    /// </summary>
    public ImmutableArray<SemanticLossFinding> Unobserved { get; init; } = [];

    /// <summary>
    /// Gets every finding, in rule order.
    /// </summary>
    public ImmutableArray<SemanticLossFinding> Findings =>
    [
        .. Malformed, .. Unledgered, .. Stale, .. Unexplained, .. Uncited, .. OverBudget,
        .. Vacuous, .. Orphans, .. IneligibleExclusions, .. ObsoleteExclusions, .. Unobserved,
    ];

    /// <summary>
    /// Gets a value indicating whether every rule holds.
    /// </summary>
    public bool IsClean => Findings.IsEmpty;
}

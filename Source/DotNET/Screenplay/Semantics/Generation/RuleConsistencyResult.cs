// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

/// <summary>
/// Describes the consistency of a generation shape with a validation rule.
/// </summary>
public enum RuleConsistencyStatus
{
    /// <summary>
    /// Every generated value satisfies the rule.
    /// </summary>
    Proven,

    /// <summary>
    /// A generated value violates the rule.
    /// </summary>
    Refuted,

    /// <summary>
    /// No proof was possible; passing samples are not proof.
    /// </summary>
    Unproven
}

/// <summary>
/// Represents a version-independent generation-rule proof outcome.
/// </summary>
/// <param name="Status">The proof status.</param>
/// <param name="Witness">A violating generated value, for a refutation.</param>
/// <param name="Reason">The reason a proof was not possible.</param>
public sealed record RuleConsistencyResult(RuleConsistencyStatus Status, string? Witness = null, string? Reason = null)
{
    internal static RuleConsistencyResult Proven => new(RuleConsistencyStatus.Proven);
    internal static RuleConsistencyResult Refuted(string witness) => new(RuleConsistencyStatus.Refuted, witness);
    internal static RuleConsistencyResult Unproven(string reason) => new(RuleConsistencyStatus.Unproven, Reason: reason);
}

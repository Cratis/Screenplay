// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents an allow, deny, or explicitly unevaluable policy predicate.
/// </summary>
/// <param name="Outcome">The answer.</param>
/// <param name="Diagnostic">Optional provider diagnostic; it is invalid on allow or deny.</param>
public sealed record SemanticPolicyBodyResult(SemanticPolicyBodyOutcome Outcome, string? Diagnostic = null)
{
    /// <summary>
    /// Creates an allowance.
    /// </summary>
    /// <returns>An allow answer.</returns>
    public static SemanticPolicyBodyResult Allow() => new(SemanticPolicyBodyOutcome.Allow);

    /// <summary>
    /// Creates a denial.
    /// </summary>
    /// <returns>A deny answer.</returns>
    public static SemanticPolicyBodyResult Deny() => new(SemanticPolicyBodyOutcome.Deny);

    /// <summary>
    /// Creates an unevaluable answer.
    /// </summary>
    /// <param name="diagnostic">An optional diagnostic.</param>
    /// <returns>An unsupported answer.</returns>
    public static SemanticPolicyBodyResult Unsupported(string? diagnostic = null) => new(SemanticPolicyBodyOutcome.Unsupported, diagnostic);
}

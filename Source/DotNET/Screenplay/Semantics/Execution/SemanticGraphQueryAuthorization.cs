// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Decides whether a caller may run a query that reads a graph, with the same policy evaluation every other query uses, so a graph query
/// is no more open than the policy it declares.
/// </summary>
public static class SemanticGraphQueryAuthorization
{
    /// <summary>
    /// Decides whether the caller may run the query.
    /// </summary>
    /// <param name="query">The graph query.</param>
    /// <param name="plan">The execution plan of the model, which carries the declared policies.</param>
    /// <param name="caller">The caller.</param>
    /// <returns><c>null</c> when the caller may run it; otherwise why not. A policy whose predicate only a target provider can evaluate refuses with its reason.</returns>
    public static string? Refusal(SemanticGraphQuery query, SemanticExecutionPlan plan, SemanticCaller caller)
    {
        var decision = SemanticPolicyEvaluation.Evaluate(query.Authorization, plan, caller, new Dictionary<string, SemanticValue>(StringComparer.Ordinal), null, []);
        return decision.Outcome switch
        {
            SemanticPolicyOutcome.Allow => null,
            SemanticPolicyOutcome.Unsupported => decision.Details ?? $"Policy '{decision.Policy}' has an opaque predicate and requires a target provider.",
            _ => "Caller is not authorized."
        };
    }
}

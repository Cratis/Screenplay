// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Indexing;

namespace Cratis.Screenplay.Comparison;

/// <summary>A separate, fail-closed comparison of authored agent exposure, not endpoint or authorization behavior.</summary>
/// <param name="Coverage">Whether every exposure row could be compared.</param>
/// <param name="Rows">Rows in stable operation-kind and qualified-name order.</param>
public sealed record AgentExposureComparison(AgentExposureCoverage Coverage, ImmutableArray<AgentExposureChange> Rows)
{
    internal const string LegacyLimit = "Agent exposure was not compared; use the agent-exposure view.";

    /// <summary>Gets whether exposure widens, or <c>null</c> when incomplete coverage prohibits that conclusion.</summary>
    public bool? HasWidening => Coverage == AgentExposureCoverage.Complete
        ? Rows.Any(row => row.Change is AgentExposureChangeKind.Added or AgentExposureChangeKind.Widened)
        : null;

    internal bool RequiresLegacyLimit { get; init; }

    internal static AgentExposureComparison Compare(AuthoringSnapshot beforeSource, AuthoringSnapshot afterSource)
    {
        var baseline = AgentExposureSnapshot.Read(beforeSource);
        var candidate = AgentExposureSnapshot.Read(afterSource);
        var sourceErrors = baseline.HasErrors || candidate.HasErrors;
        var rows = baseline.Operations.Keys.Union(candidate.Operations.Keys)
            .OrderBy(key => key.Kind).ThenBy(key => key.Name, StringComparer.Ordinal)
            .Select(key => Compare(key, baseline, candidate, sourceErrors)).ToImmutableArray();
        return new(sourceErrors ? AgentExposureCoverage.Incomplete : AgentExposureCoverage.Complete, rows)
        {
            RequiresLegacyLimit = sourceErrors || baseline.HasBlock || candidate.HasBlock
        };
    }

    static AgentExposureChange Compare(AgentExposureSnapshot.Key key, AgentExposureSnapshot before, AgentExposureSnapshot after, bool sourceErrors)
    {
        before.Operations.TryGetValue(key, out var baseline);
        after.Operations.TryGetValue(key, out var candidate);
        var (change, reason) = Classify(baseline, candidate, before.Tokens != after.Tokens, sourceErrors);
        var diagnostic = change switch
        {
            AgentExposureChangeKind.Added => DiagnosticCodes.NewlyExposedAgentOperation,
            AgentExposureChangeKind.Widened => DiagnosticCodes.WidenedAgentOperation,
            _ => null
        };
        return new(key.Kind, key.Name, change, reason, baseline?.Scope, candidate?.Scope, diagnostic);
    }

    static (AgentExposureChangeKind Change, string Reason) Classify(AgentExposureSnapshot.Operation? before, AgentExposureSnapshot.Operation? after, bool tokensChanged, bool sourceErrors)
    {
        if (sourceErrors) return (AgentExposureChangeKind.Unknown, "Parsing or validation errors prevent exposure comparison on either source.");
        if (before is null) return (AgentExposureChangeKind.Added, "The operation becomes explicitly exposed.");
        if (after is null) return (AgentExposureChangeKind.Removed, "The operation is no longer exposed.");
        if (tokensChanged) return (AgentExposureChangeKind.Widened, "The accepted token contract changes; review as widened exposure.");
        if (before.Signature is null || after.Signature is null) return (AgentExposureChangeKind.Widened, "The exposed contract changed or could not be uniquely resolved; review as widened exposure.");
        if (before.Signature != after.Signature) return (AgentExposureChangeKind.Widened, "The exposed contract changed (operation signature or referenced contract).");
        if (after.Scope > before.Scope) return (AgentExposureChangeKind.Widened, "The required token scope increases.");
        if (after.Scope < before.Scope) return (AgentExposureChangeKind.Narrowed, "Only the required token scope decreases.");
        return (AgentExposureChangeKind.Unchanged, "The resolved target, signature and exposure metadata are unchanged; descriptions do not widen exposure.");
    }
}

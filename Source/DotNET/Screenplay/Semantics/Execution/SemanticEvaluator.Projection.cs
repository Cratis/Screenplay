// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

public sealed partial class SemanticEvaluator
{
    internal static bool TryProject(
        SemanticExecutionPlan plan,
        ImmutableArray<SemanticFact> history,
        ImmutableArray<SemanticReadModelInstance> current,
        ImmutableArray<SemanticFact> facts,
        out ImmutableArray<SemanticReadModelInstance> readModels,
        out string? failure) =>
        TryProject(plan, history, current, facts, [], out readModels, out _, out failure);

    internal static bool TryProject(
        SemanticExecutionPlan plan,
        ImmutableArray<SemanticFact> history,
        ImmutableArray<SemanticReadModelInstance> current,
        ImmutableArray<SemanticFact> facts,
        ImmutableArray<SemanticScopedProjection.DeferredChild> pending,
        out ImmutableArray<SemanticReadModelInstance> readModels,
        out ImmutableArray<SemanticScopedProjection.DeferredChild> remaining,
        out string? failure)
    {
        remaining = pending;
        if (SemanticDeferredProjections.FindFailure(plan, facts) is { } deferredFailure)
        {
            failure = deferredFailure;
            readModels = current;
            return false;
        }

        var instances = current.ToList();
        var concepts = plan.Model.Application.Concepts.ToDictionary(_ => _.Id);
        var types = plan.Model.Application.Types.ToDictionary(_ => _.Id);
        var validator = new SemanticValueValidator(concepts, types);
        var observed = history.ToList();
        var deferred = pending.ToList();
        var projection = new SemanticFactProjection(plan, validator, instances, observed, deferred);

        foreach (var fact in facts)
        {
            if (projection.Apply(fact) is { } projectionFailure)
            {
                failure = projectionFailure;
                readModels = current;
                return false;
            }

            observed.Add(fact);
        }

        failure = null;
        readModels = [.. instances];
        remaining = [.. deferred];
        return true;
    }
}

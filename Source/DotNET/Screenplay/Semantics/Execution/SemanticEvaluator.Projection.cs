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
        out string? failure)
    {
        var projected = TryProject(plan, history, new ProjectionState(current, []), facts, out var state, out failure, SemanticReducerBodyExecution.None, CancellationToken.None);
        readModels = state.ReadModels;
        return projected;
    }

    internal static bool TryProject(
        SemanticExecutionPlan plan,
        ImmutableArray<SemanticFact> history,
        ProjectionState current,
        ImmutableArray<SemanticFact> facts,
        out ProjectionState state,
        out string? failure) =>
        TryProject(plan, history, current, facts, out state, out failure, SemanticReducerBodyExecution.None, CancellationToken.None);

    internal static bool TryProject(
        SemanticExecutionPlan plan,
        ImmutableArray<SemanticFact> history,
        ProjectionState current,
        ImmutableArray<SemanticFact> facts,
        out ProjectionState state,
        out string? failure,
        SemanticReducerBodyExecution reducers,
        CancellationToken cancellationToken)
    {
        state = current;
        if (SemanticDeferredProjections.FindFailure(plan, facts) is { } deferredFailure)
        {
            failure = deferredFailure;
            return false;
        }

        var instances = current.ReadModels.ToList();
        var concepts = plan.Model.Application.Concepts.ToDictionary(_ => _.Id);
        var types = plan.Model.Application.Types.ToDictionary(_ => _.Id);
        var validator = new SemanticValueValidator(concepts, types);
        var observed = history.ToList();
        var deferred = current.PendingChildren.ToList();
        var projection = new SemanticFactProjection(plan, validator, instances, observed, deferred);

        foreach (var fact in facts)
        {
            if (projection.Apply(fact) is { } projectionFailure)
            {
                failure = projectionFailure;
                return false;
            }

            if (reducers.Apply(plan, new(fact, (ulong)observed.Count), instances, cancellationToken) is { } reducerFailure)
            {
                failure = reducerFailure;
                return false;
            }

            observed.Add(fact);
        }

        failure = null;
        state = new([.. instances], [.. deferred]);
        return true;
    }

    internal sealed record ProjectionState(
        ImmutableArray<SemanticReadModelInstance> ReadModels,
        ImmutableArray<SemanticScopedProjection.DeferredChild> PendingChildren);
}

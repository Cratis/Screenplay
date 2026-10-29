// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed partial class SemanticReactionLoop
{
    SemanticExecutionResult? ProduceAll(
        SemanticReaction reaction,
        ImmutableArray<SemanticProducedEvent> productions,
        Dictionary<SemanticId, SemanticValue> values,
        SemanticExpressionRootKind root,
        SemanticFact? cause,
        SemanticCommandOccurrence? occurrence,
        SemanticInvocationRefusalEvaluation? refusal = null)
    {
        if (plan.Model.Application.ReactionProductionMode is null)
        {
            foreach (var produced in productions)
            {
                if (Produce(reaction, produced, values, root, cause, occurrence, refusal) is { } failure) return failure;
            }
            return null;
        }

        if (productions.IsEmpty) return null;
        var candidates = ImmutableArray.CreateBuilder<SemanticFact>();
        foreach (var produced in productions)
        {
            if (Materialize(reaction, produced, values, root, cause, occurrence, refusal, out var fact) is { } failure) return failure;
            candidates.Add(fact!);
        }

        var result = PrepareBatch(candidates.ToImmutable());
        return result is SemanticAccepted accepted ? AcceptCommand(accepted, occurrence?.Occurred) : result;
    }

    SemanticExecutionResult PrepareBatch(ImmutableArray<SemanticFact> facts)
    {
        var claims = new SemanticReactionBatchClaims(plan, World);
        for (var index = 0; index < facts.Length; index++)
        {
            if (claims.Validate(facts[index]) is { } violated)
            {
                return SemanticEvaluator.RejectWithMessage(World, SemanticRejectionCategory.Constraint, violated.Name, SemanticConstraintEnforcement.MessageFor(violated)) with
                {
                    Production = new(index, facts[index].EventContract, facts[index].Route)
                };
            }
        }

        return ProjectBatch(facts);
    }

    SemanticExecutionResult ProjectBatch(ImmutableArray<SemanticFact> facts)
    {
        if (SemanticEvaluator.ReachedReducer(plan, facts, SemanticReducerBodyExecution.For(evaluator)) is { } reducer)
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Projection, $"Reducer '{reducer.Name}' has opaque transitions and requires a target provider.");
        }

        if (!SemanticEvaluator.TryProject(plan, World.Facts, new SemanticEvaluator.ProjectionState(World.ReadModels, World.PendingChildren), facts, out var state, out var failure, SemanticReducerBodyExecution.For(evaluator), SemanticReducerBodyExecution.TokenFor(evaluator)))
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Projection, failure!);
        }

        if (!SemanticEventPublication.TryPublish(plan, World.Facts, facts, out var publications, out var publicationFailure))
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Projection, publicationFailure!);
        }

        // Publications follow all primary facts, as in the command collection contract. They are
        // derived effects, not additional returned-event constraint reservations.
        var expanded = facts.AddRange(publications);
        if (SemanticDeferredProjections.FindFailure(plan, expanded) is { } deferredFailure)
        {
            return new SemanticUnsupported(World, SemanticExecutionCapability.Projection, deferredFailure);
        }

        return new SemanticAccepted(World.Commit(expanded, state.ReadModels, state.PendingChildren), expanded, []);
    }
}

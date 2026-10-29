// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticReducerBodyExecution(ISemanticBodyExecutionProvider? provider, SemanticRuleBodyExecution? bodies)
{
    internal static SemanticReducerBodyExecution None { get; } = new(null, null);

    internal static CancellationToken TokenFor(ISemanticEvaluator evaluator) =>
        evaluator is SemanticCancellationEvaluator cancelled ? cancelled.CancellationToken : CancellationToken.None;

    internal static SemanticReducerBodyExecution For(ISemanticEvaluator evaluator) => evaluator switch
    {
        SemanticEvaluator reference => reference.ReducerBodies,
        SemanticCancellationEvaluator cancelled => For(cancelled.Evaluator),
        _ => None
    };

    internal static SemanticExecutionResult EstablishSpecificationWorld(ISemanticEvaluator evaluator, SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts)
    {
        var underlying = evaluator is SemanticCancellationEvaluator cancelled ? cancelled.Evaluator : evaluator;
        return (underlying as SemanticEvaluator ?? new SemanticEvaluator()).EstablishSpecificationWorld(plan, facts, TokenFor(evaluator));
    }

    internal static bool Reached(SemanticReducer reducer, SemanticFact fact, SemanticExecutionPlan plan) =>
        reducer.Transitions.Any(transition => transition.EventContract == fact.EventContract) &&
        (reducer.From?.Matches(plan.Model.Application, fact.Route) ?? true);

    internal bool CanExecute(SemanticExecutionPlan plan, SemanticReducer reducer) =>
        provider is not null && TargetsSupportedModel(plan, reducer) &&
        !reducer.Transitions.IsEmpty && reducer.Transitions.All(transition => Resolve(reducer, transition, out _) is not null);

    internal string? InvalidIdentity(SemanticExecutionPlan plan, ImmutableArray<SemanticFact> facts)
    {
        if (facts.IsDefault) return null;
        foreach (var fact in facts.Where(fact => fact is not null && !SemanticReducerTransitionExecution.HasScalarKey(fact)))
        {
            var reducer = SemanticEvaluator.AllReducers(plan).FirstOrDefault(reducer => Reached(reducer, fact, plan) && CanExecute(plan, reducer));
            if (reducer is not null) return Unsupported(reducer, reducer.Transitions.Single(transition => transition.EventContract == fact.EventContract), "The event source key must be a non-null scalar identity.");
        }
        return null;
    }

    internal string? Apply(SemanticExecutionPlan plan, SemanticReducerFact input, List<SemanticReadModelInstance> instances, CancellationToken cancellationToken)
    {
        // Legacy paths that do not consume reducer state retain their existing admission guards and do not fold it.
        if (provider is null) return null;
        var fact = input.Fact;

        // Establishment skips non-executable reducers; state assertions and public replay keep their admission guards.
        foreach (var reducer in SemanticEvaluator.AllReducers(plan).Where(reducer => Reached(reducer, fact, plan) && CanExecute(plan, reducer)))
        {
            var transition = reducer.Transitions.Single(transition => transition.EventContract == fact.EventContract);
            var body = Resolve(reducer, transition, out var failure);
            if (body is null) return failure;
            var execution = new SemanticReducerTransitionExecution(provider, plan, plan.ReadModels[reducer.ReadModel]);
            if (execution.Apply(input, body, instances, cancellationToken) is { } unsupported) return unsupported;
        }
        return null;
    }

    static bool TargetsSupportedModel(SemanticExecutionPlan plan, SemanticReducer reducer) =>
        reducer.Target == SemanticProjectionTargetKind.ReadModel && plan.ReadModels.ContainsKey(reducer.ReadModel) &&
        !plan.Projections.Values.Any(projection => projection.ReadModel == reducer.ReadModel);

    static string Unsupported(SemanticReducer reducer, SemanticReducerTransition transition, string diagnostic) =>
        $"Reducer '{reducer.Name}' transition (requirement {transition.RequirementId}): {SemanticRuleBodyExecution.Sanitize(diagnostic)}";

    SemanticImplementationBody? Resolve(SemanticReducer reducer, SemanticReducerTransition transition, out string? failure)
    {
        var body = bodies?.FindBody(transition.RequirementId, SemanticImplementationRole.ReducerTransition);
        failure = null;
        if (provider is null) failure = $"Reducer '{reducer.Name}' has opaque transitions and requires a target provider.";
        else if (body is null) failure = Unsupported(reducer, transition, "No resolved body is available.");
        else if (!SemanticRuleBodyExecution.MatchesHash(body)) failure = Unsupported(reducer, transition, "Resolved body content does not match its requirement hash.");
        return failure is null ? body : null;
    }
}

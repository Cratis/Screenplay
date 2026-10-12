// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Answers keyed queries of an execution request against a tentative world.
/// </summary>
/// <param name="plan">The capability-admitted plan.</param>
/// <param name="original">The world the request started from.</param>
/// <param name="tentative">The world the queries read.</param>
/// <param name="caller">The caller, when one is supplied.</param>
sealed class SemanticQueryRun(
    SemanticExecutionPlan plan,
    SemanticWorld original,
    SemanticWorld tentative,
    SemanticCaller? caller)
{
    /// <summary>
    /// Answers one query request.
    /// </summary>
    /// <param name="queryRequest">The query request.</param>
    /// <param name="result">The answer when the query is accepted.</param>
    /// <returns>The refusal, or <see langword="null"/> when the query was answered.</returns>
    public SemanticExecutionResult? Answer(SemanticQueryRequest queryRequest, out SemanticQueryResult? result)
    {
        result = null;
        if (!plan.Queries.TryGetValue(queryRequest.Query, out var query))
        {
            return new SemanticUnsupported(original, SemanticExecutionCapability.Query, $"Query '{queryRequest.Query}' is not in the execution plan.");
        }

        if (plan.UnadmittedArtifacts.Contains(query.Id) || plan.IsUnadmittedReadModel(query.ReadModel))
        {
            return new SemanticUnsupported(original, SemanticExecutionCapability.Query, $"Query '{query.Name}' or its read model was not admitted by scoped execution-plan compilation.");
        }

        if (RefuseReducer(query.ReadModel, SemanticExecutionCapability.Query) is { } reducerRefusal)
        {
            return reducerRefusal;
        }

        if (Authorize(query, queryRequest.Key) is { } authorizationRefusal)
        {
            return authorizationRefusal;
        }

        if ((SemanticEvaluator.ValidateQueryKey(plan, query, queryRequest.Key) ??
             SemanticEvaluator.ValidateQueryFilters(plan, query, queryRequest.Filters)) is { } queryRejection)
        {
            return new SemanticRejected(original, SemanticRejectionCategory.Contract, null, queryRejection);
        }

        var results = tentative.ReadModels
            .Where(instance => instance.ReadModel == query.ReadModel &&
                (query.KeyProperty is null || instance.Values.Any(value =>
                    value.TargetProperty == query.KeyProperty && SemanticValueRules.AreEqual(value.Value, queryRequest.Key))) &&
                queryRequest.Filters.All(filterValue => instance.Values.Any(value =>
                value.TargetProperty == query.Filters.Single(filter => filter.Id == filterValue.Filter).Property &&
                SemanticValueRules.AreEqual(value.Value, filterValue.Value))))
            .ToImmutableArray();
        result = new(query.Id, queryRequest.Key, results)
        {
            Filters = [.. query.Filters.Select(filter => queryRequest.Filters.SingleOrDefault(value => value.Filter == filter.Id))
                .OfType<SemanticQueryFilterValue>()]
        };
        return null;
    }

    /// <summary>
    /// Refuses when any read model in the tentative world is built by a reducer.
    /// </summary>
    /// <returns>The refusal, or <see langword="null"/> when no read model is reducer-backed.</returns>
    public SemanticExecutionResult? RefuseReducerBackedWorld()
    {
        foreach (var instance in tentative.ReadModels)
        {
            if (plan.IsUnadmittedReadModel(instance.ReadModel))
            {
                return new SemanticUnsupported(original, SemanticExecutionCapability.Projection, $"Read model '{instance.ReadModel}' was not admitted by scoped execution-plan compilation.");
            }

            if (RefuseReducer(instance.ReadModel, SemanticExecutionCapability.Projection) is { } refusal)
            {
                return refusal;
            }
        }

        return null;
    }

    SemanticUnsupported? RefuseReducer(SemanticId readModel, SemanticExecutionCapability capability) =>
        SemanticEvaluator.ReducerFor(plan, readModel) is { } reducer
            ? new SemanticUnsupported(
                original,
                capability,
                $"Reducer '{reducer.Name}' has opaque transitions and requires a target provider to compute read-model state.")
            : null;

    SemanticExecutionResult? Authorize(SemanticKeyedQuery query, SemanticValue key)
    {
        var arguments = query.Argument is null
            ? new Dictionary<string, SemanticValue>(StringComparer.Ordinal)
            : new Dictionary<string, SemanticValue>(StringComparer.Ordinal) { [query.Argument.Name] = key };
        var properties = query.Argument is null
            ? []
            : new[] { new SemanticProperty(query.Argument.Id, query.Argument.Name, query.Argument.Type, false) };
        var authorization = SemanticPolicyEvaluation.Evaluate(query.Authorization, plan, caller, arguments, key, properties);
        return authorization.Outcome switch
        {
            SemanticPolicyOutcome.Unsupported => new SemanticUnsupported(
                original,
                SemanticExecutionCapability.Authorization,
                $"Policy '{authorization.Policy}' has an opaque predicate and requires a target provider."),
            SemanticPolicyOutcome.Deny => new SemanticRejected(original, SemanticRejectionCategory.Unauthorized, null, "Caller is not authorized."),
            _ => null
        };
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Runs executable semantic specifications through the reference evaluator.
/// </summary>
public interface ISemanticSpecificationRunner
{
    /// <summary>
    /// Executes and compares one specification from the plan.
    /// </summary>
    /// <param name="plan">The capability-admitted plan.</param>
    /// <param name="specification">The specification semantic identity.</param>
    /// <returns>The normalized execution and deterministic comparison failures.</returns>
    SemanticSpecificationRun Run(SemanticExecutionPlan plan, SemanticId specification);
}

/// <summary>
/// Represents one normalized reference execution of a semantic specification.
/// </summary>
/// <param name="Specification">The specification semantic identity.</param>
/// <param name="Passed">Whether every authored expectation matched.</param>
/// <param name="Execution">The normalized command execution result.</param>
/// <param name="Failures">Expectation failures in deterministic comparison order.</param>
public sealed record SemanticSpecificationRun(
    SemanticId Specification,
    bool Passed,
    SemanticExecutionResult Execution,
    ImmutableArray<string> Failures);

/// <summary>
/// Executes semantic specifications against immutable in-memory world state.
/// </summary>
/// <param name="evaluator">The reference semantic evaluator.</param>
public sealed class SemanticSpecificationRunner(ISemanticEvaluator evaluator) : ISemanticSpecificationRunner
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticSpecificationRunner"/> class with the reference evaluator.
    /// </summary>
    public SemanticSpecificationRunner()
        : this(new SemanticEvaluator())
    {
    }

    /// <inheritdoc/>
    public SemanticSpecificationRun Run(SemanticExecutionPlan plan, SemanticId specification)
    {
        if (!plan.Specifications.TryGetValue(specification, out var expected))
        {
            var unsupported = new SemanticUnsupported(
                SemanticWorld.Empty,
                SemanticExecutionCapability.Specification,
                $"Specification '{specification}' is not in the execution plan.");
            return new(specification, false, unsupported, [unsupported.Details]);
        }

        var reducer = plan.Model.Application.Modules.SelectMany(module => AllSlices(module.Features))
            .SelectMany(slice => slice.Reducers)
            .FirstOrDefault(candidate =>
                expected.GivenReadModels.Any(state => state.ReadModel == candidate.ReadModel) ||
                expected.ThenReadModels.Any(state => state.ReadModel == candidate.ReadModel) ||
                expected.ThenQueries.Any(query => plan.Queries.TryGetValue(query.Query, out var target) && target.ReadModel == candidate.ReadModel));
        if (reducer is not null)
        {
            var unsupported = new SemanticUnsupported(
                SemanticWorld.Empty,
                SemanticExecutionCapability.Projection,
                $"Reducer '{reducer.Name}' has opaque transitions and requires a target provider to compute read-model state.");
            return new(specification, false, unsupported, [unsupported.Details]);
        }

        if (EstablishWorld(plan, expected, out var establishmentFailure) is not { } world)
        {
            var unsupported = new SemanticUnsupported(
                SemanticWorld.Empty,
                SemanticExecutionCapability.Projection,
                establishmentFailure!);
            return new(specification, false, unsupported, [unsupported.Details]);
        }

        var queries = expected.ThenQueries.Select(value => new SemanticQueryRequest(value.Query, value.Key)).ToImmutableArray();
        var request = expected.When is null
            ? SemanticExecutionRequest.ForQueries(queries)
            : SemanticExecutionRequest.Create(expected.When.Command, expected.When.Values, queries) with
            {
                AllocatedIdentities = expected.When.EventSource is null
                    ? ImmutableDictionary.Create<SemanticId, SemanticValue>()
                    : ImmutableDictionary<SemanticId, SemanticValue>.Empty.Add(expected.When.Command, expected.When.EventSource.Value),
                AllocatedEventSourceType = expected.When.EventSource?.Type
            };

        // An append is an occurrence, not a command: enforce append constraints, project, then query.
        // Reactions are not part of the ESM and are deliberately not executed here.
        var execution = expected.WhenAppended is { } appended
            ? SemanticEvaluator.Append(
                plan,
                world,
                new SemanticFact(
                appended.EventContract,
                appended.EventSource?.Value ?? SemanticValue.Null,
                appended.Values)
            {
                Context = appended.EventSource is null ? null : new(appended.EventSource)
            },
                queries,
                expected.GivenCaller)
            : evaluator.Execute(plan, world, request with { Caller = expected.GivenCaller });
        var failures = Compare(expected, execution);
        return new(specification, failures.IsEmpty, execution, failures);
    }

    static IEnumerable<SemanticSlice> AllSlices(ImmutableArray<SemanticFeature> features) =>
        features.SelectMany(feature => feature.Slices.Concat(AllSlices(feature.Features)));

    static SemanticWorld? EstablishWorld(
        SemanticExecutionPlan plan,
        SemanticSpecification specification,
        out string? failure)
    {
        var facts = specification.GivenEvents
            .Select(value => new SemanticFact(value.EventContract, value.EventSource?.Value ?? SemanticValue.Null, value.Values)
            {
                Context = value.EventSource is null ? null : new(value.EventSource)
            })
            .ToImmutableArray();
        if (!SemanticEvaluator.Establish(plan, [], facts, out var projected, out failure))
        {
            return null;
        }

        var readModels = projected.ToList();
        foreach (var state in specification.GivenReadModels)
        {
            var existing = readModels.SingleOrDefault(value =>
                value.ReadModel == state.ReadModel && SemanticValueRules.AreEqual(value.Key, state.Key));
            if (existing is not null)
            {
                readModels.Remove(existing);
            }

            readModels.Add(new(state.ReadModel, state.Key, state.Values));
        }

        failure = null;
        return SemanticWorld.Create(facts, [.. readModels]);
    }

    static ImmutableArray<string> Compare(
        SemanticSpecification expected,
        SemanticExecutionResult execution)
    {
        var failures = ImmutableArray.CreateBuilder<string>();
        if (expected.ThenDenied)
        {
            if (execution is not SemanticRejected { Category: SemanticRejectionCategory.Unauthorized })
            {
                failures.Add($"Expected Unauthorized, got {execution.Kind}{(execution is SemanticRejected rejected ? $" ({rejected.Category})" : string.Empty)}.");
            }

            return failures.ToImmutable();
        }

        if (expected.ThenErrors.Length > 0)
        {
            CompareRejection(expected, execution, failures);
            return failures.ToImmutable();
        }

        if (execution is not SemanticAccepted accepted)
        {
            failures.Add($"Expected Accepted, got {execution.Kind}.");
            return failures.ToImmutable();
        }

        if (expected.WhenAppended is null || expected.ThenEvents.Length > 0)
        {
            CompareFacts(expected.ThenEvents, accepted.Facts, failures, expected.ThenEventsInAnyOrder);
        }
        if (expected.When?.EventSource is { } commandSource && accepted.Facts.Any(fact => !SemanticValueRules.AreEqual(fact.Destination, commandSource.Value)))
        {
            failures.Add("Produced fact destination does not match the specification command event source.");
        }
        CompareReadModels(expected.ThenReadModels, accepted.World.ReadModels, failures, "read model");
        CompareQueries(expected.ThenQueries, accepted.Queries, failures);
        return failures.ToImmutable();
    }

    static void CompareRejection(
        SemanticSpecification expected,
        SemanticExecutionResult execution,
        ImmutableArray<string>.Builder failures)
    {
        if (execution is not SemanticRejected rejected || rejected.Category == SemanticRejectionCategory.Unauthorized)
        {
            failures.Add($"Expected Rejected, got {execution.Kind}.");
            return;
        }

        var error = expected.ThenErrors.Single();
        if (error.Code is not null && error.Code != rejected.Code)
        {
            failures.Add($"Expected rejection code '{error.Code}', got '{rejected.Code}'.");
        }

        if (error.Message is not null &&
            (error.Message != rejected.Details ||
             error.Message.StartsWith("$strings.", StringComparison.Ordinal) != rejected.MessageIsStringKey))
        {
            failures.Add($"Expected rejection message '{error.Message}', got '{rejected.Details}' (string key: {rejected.MessageIsStringKey}).");
        }
    }

    static void CompareFacts(
        ImmutableArray<SemanticSpecificationEvent> expected,
        ImmutableArray<SemanticFact> actual,
        ImmutableArray<string>.Builder failures,
        bool inAnyOrder)
    {
        if (expected.Length != actual.Length)
        {
            failures.Add($"Expected {expected.Length} fact(s), got {actual.Length}.");
            return;
        }

        var remaining = actual.ToList();
        for (var index = 0; index < expected.Length; index++)
        {
            var matched = inAnyOrder ? remaining.FindIndex(fact => FactMatches(expected[index], fact)) : index;
            if (matched < 0 || !FactMatches(expected[index], inAnyOrder ? remaining[matched] : actual[index]))
            {
                failures.Add($"Fact at index {index} does not match the expected event contract and values.");
            }

            if (inAnyOrder && matched >= 0) remaining.RemoveAt(matched);
        }
    }

    static bool FactMatches(SemanticSpecificationEvent expected, SemanticFact actual) =>
        expected.EventContract == actual.EventContract && ValuesEqual(expected.Values, actual.Values) &&
        (expected.EventSource is null ||
         (actual.Context?.EventSource.Type == expected.EventSource.Type &&
          SemanticValueRules.AreEqual(actual.Destination, expected.EventSource.Value)));

    static void CompareReadModels(
        ImmutableArray<SemanticSpecificationReadModel> expected,
        ImmutableArray<SemanticReadModelInstance> actual,
        ImmutableArray<string>.Builder failures,
        string description,
        bool exactly = false)
    {
        foreach (var state in expected)
        {
            var match = actual.SingleOrDefault(value =>
                value.ReadModel == state.ReadModel && SemanticValueRules.AreEqual(value.Key, state.Key));
            if (match is null || !(exactly || state.Exactly ? ValuesEqual(state.Values, match.Values) : ValuesContain(state.Values, match.Values)))
            {
                failures.Add($"Expected {description} '{state.ReadModel}' with key '{state.Key}' was not found with matching values.");
            }
        }
    }

    static void CompareQueries(
        ImmutableArray<SemanticSpecificationQueryResult> expected,
        ImmutableArray<SemanticQueryResult> actual,
        ImmutableArray<string>.Builder failures)
    {
        if (expected.Length != actual.Length)
        {
            failures.Add($"Expected {expected.Length} query result(s), got {actual.Length}.");
            return;
        }

        for (var index = 0; index < expected.Length; index++)
        {
            if (expected[index].Query != actual[index].Query || !SemanticValueRules.AreEqual(expected[index].Key, actual[index].Key))
            {
                failures.Add($"Query result at index {index} identifies the wrong query or key.");
                continue;
            }

            CompareReadModels(expected[index].Results, actual[index].Results, failures, $"query result at index {index}", expected[index].Exactly);
            if (expected[index].Results.Length != actual[index].Results.Length)
            {
                failures.Add($"Query result at index {index} expected {expected[index].Results.Length} row(s), got {actual[index].Results.Length}.");
            }
        }
    }

    // A missing property is not null: only a present SemanticNullValue matches an asserted null.
    static bool ValuesContain(
        ImmutableArray<SemanticPropertyValue> expected,
        ImmutableArray<SemanticPropertyValue> actual) =>
        expected.All(value =>
            actual.SingleOrDefault(candidate => candidate.TargetProperty == value.TargetProperty) is { } candidate &&
            SemanticValueRules.AreEqual(value.Value, candidate.Value));

    static bool ValuesEqual(
        ImmutableArray<SemanticPropertyValue> expected,
        ImmutableArray<SemanticPropertyValue> actual) =>
        expected.Length == actual.Length && expected.All(value =>
            actual.SingleOrDefault(candidate => candidate.TargetProperty == value.TargetProperty) is { } candidate &&
            SemanticValueRules.AreEqual(value.Value, candidate.Value));
}

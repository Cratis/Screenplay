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

        var world = SemanticWorld.Create(
            [.. expected.GivenEvents.Select(value => new SemanticFact(value.EventContract, SemanticValue.Null, value.Values))],
            [.. expected.GivenReadModels.Select(value => new SemanticReadModelInstance(value.ReadModel, value.Key, value.Values))]);
        var request = SemanticExecutionRequest.Create(
            expected.When.Command,
            expected.When.Values,
            [.. expected.ThenQueries.Select(value => new SemanticQueryRequest(value.Query, value.Key))]);
        var execution = evaluator.Execute(plan, world, request);
        var failures = Compare(expected, execution);
        return new(specification, failures.IsEmpty, execution, failures);
    }

    static ImmutableArray<string> Compare(
        SemanticSpecification expected,
        SemanticExecutionResult execution)
    {
        var failures = ImmutableArray.CreateBuilder<string>();
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

        CompareFacts(expected.ThenEvents, accepted.Facts, failures);
        CompareReadModels(expected.ThenReadModels, accepted.World.ReadModels, failures, "read model");
        CompareQueries(expected.ThenQueries, accepted.Queries, failures);
        return failures.ToImmutable();
    }

    static void CompareRejection(
        SemanticSpecification expected,
        SemanticExecutionResult execution,
        ImmutableArray<string>.Builder failures)
    {
        if (execution is not SemanticRejected rejected)
        {
            failures.Add($"Expected Rejected, got {execution.Kind}.");
            return;
        }

        var error = expected.ThenErrors.Single();
        if (error.Code is not null && error.Code != rejected.Code)
        {
            failures.Add($"Expected rejection code '{error.Code}', got '{rejected.Code}'.");
        }

        if (error.Message is not null && error.Message != rejected.Details)
        {
            failures.Add($"Expected rejection message '{error.Message}', got '{rejected.Details}'.");
        }
    }

    static void CompareFacts(
        ImmutableArray<SemanticSpecificationEvent> expected,
        ImmutableArray<SemanticFact> actual,
        ImmutableArray<string>.Builder failures)
    {
        if (expected.Length != actual.Length)
        {
            failures.Add($"Expected {expected.Length} fact(s), got {actual.Length}.");
            return;
        }

        for (var index = 0; index < expected.Length; index++)
        {
            if (expected[index].EventContract != actual[index].EventContract || !ValuesEqual(expected[index].Values, actual[index].Values))
            {
                failures.Add($"Fact at index {index} does not match the expected event contract and values.");
            }
        }
    }

    static void CompareReadModels(
        ImmutableArray<SemanticSpecificationReadModel> expected,
        ImmutableArray<SemanticReadModelInstance> actual,
        ImmutableArray<string>.Builder failures,
        string description)
    {
        foreach (var state in expected)
        {
            var match = actual.SingleOrDefault(value =>
                value.ReadModel == state.ReadModel && SemanticValueRules.AreEqual(value.Key, state.Key));
            if (match is null || !ValuesEqual(state.Values, match.Values))
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

            CompareReadModels(expected[index].Results, actual[index].Results, failures, $"query result at index {index}");
            if (expected[index].Results.Length != actual[index].Results.Length)
            {
                failures.Add($"Query result at index {index} expected {expected[index].Results.Length} row(s), got {actual[index].Results.Length}.");
            }
        }
    }

    static bool ValuesEqual(
        ImmutableArray<SemanticPropertyValue> expected,
        ImmutableArray<SemanticPropertyValue> actual) =>
        expected.Length == actual.Length && expected.All(value =>
            actual.SingleOrDefault(candidate => candidate.TargetProperty == value.TargetProperty) is { } candidate &&
            SemanticValueRules.AreEqual(value.Value, candidate.Value));
}

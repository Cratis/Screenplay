// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Printing;

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
/// <remarks>
/// Unlike public world establishment, given events without a source retain their legacy null destination,
/// and events observed by a reducer do not make a specification unsupported unless it asserts or queries
/// that reducer's read-model state.
/// </remarks>
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

    /// <summary>
    /// Executes a source-bound specification and enriches failures with its effective fixture values and origins.
    /// </summary>
    /// <param name="compilation">The validated source compilation owning both the ESM and its provenance sidecar.</param>
    /// <param name="specification">The specification semantic identity.</param>
    /// <returns>The reference execution, or a failed unsupported result when plan admission fails.</returns>
    public SemanticSpecificationRun Run(SemanticCompilation compilation, SemanticId specification)
    {
        var admitted = SemanticExecutionPlan.Compile(compilation.Model);
        if (admitted.Plan is not { } plan)
        {
            var details = string.Join("; ", admitted.Issues.Select(issue => issue.Details));
            var unsupported = new SemanticUnsupported(SemanticWorld.Empty, SemanticExecutionCapability.Specification, details);
            return new(specification, false, unsupported, [details]);
        }

        return EnrichFailures(compilation, Run(plan, specification));
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
                expected.ThenAbsentReadModels.Any(state => state.ReadModel == candidate.ReadModel) ||
                expected.ThenQueries.Any(query => plan.Queries.TryGetValue(query.Query, out var target) && target.ReadModel == candidate.ReadModel));
        if (reducer is not null)
        {
            var unsupported = new SemanticUnsupported(
                SemanticWorld.Empty,
                SemanticExecutionCapability.Projection,
                $"Reducer '{reducer.Name}' has opaque transitions and requires a target provider to compute read-model state.");
            return new(specification, false, unsupported, [unsupported.Details]);
        }

        var establishment = EstablishWorld(plan, expected, out var world);
        if (establishment is not SemanticAccepted)
        {
            var details = establishment switch
            {
                SemanticUnsupported unsupported => unsupported.Details,
                SemanticRejected rejected => rejected.Details,
                _ => "World establishment did not complete."
            };
            return new(specification, false, establishment, [details]);
        }

        var queries = expected.ThenQueries.Select(value => new SemanticQueryRequest(value.Query, value.Key)).ToImmutableArray();

        // Since ESM v6 an action sets reactions off, and a specification can act through the clock, a trigger or a capture.
        var acts = expected.When is not null || expected.WhenAppended is not null || expected.WhenClock is not null ||
            expected.WhenTrigger is not null || expected.WhenCapture is not null;
        if (plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V6) && acts)
        {
            var performed = SemanticScenario.Perform(evaluator, plan, world, expected, queries, out var actionFacts);
            var compared = Compare(plan, expected, performed, actionFacts);
            return new(specification, compared.IsEmpty, performed, compared);
        }

        var request = expected.When is null
            ? SemanticExecutionRequest.ForQueries(queries)
            : SemanticExecutionRequest.Create(expected.When.Command, expected.When.Values, queries) with
            {
                AllocatedIdentities = expected.When.EventSource is null
                    ? ImmutableDictionary.Create<SemanticId, SemanticValue>()
                    : ImmutableDictionary<SemanticId, SemanticValue>.Empty.Add(expected.When.Command, expected.When.EventSource.Value),
                AllocatedEventSourceType = expected.When.EventSource?.Type,
                GeneratedValues = expected.When.GeneratedValues
            };

        // An append is an occurrence, not a command: enforce append constraints, project, then query.
        // Models before ESM v6 have no reactions to run.
        var execution = expected.WhenAppended is { } appended
            ? SemanticEvaluator.Append(
                plan,
                world,
                new SemanticFact(
                appended.EventContract,
                appended.EventSource?.Value ?? SemanticValue.Null,
                appended.Values)
                {
                    Context = appended.EventSource is null ? null : new(appended.EventSource),
                    Route = plan.FormatFixtureRoute(appended.Route)
                },
                queries,
                expected.GivenCaller)
            : evaluator.Execute(plan, world, request with { Caller = expected.GivenCaller });
        var failures = Compare(plan, expected, execution);
        return new(specification, failures.IsEmpty, execution, failures);
    }

    internal static SemanticSpecificationRun EnrichFailures(SemanticCompilation compilation, SemanticSpecificationRun run)
    {
        if (run.Passed || !compilation.SpecificationOrigins.TryGetValue(run.Specification, out var origin) ||
            (origin.Case is null && !origin.Steps.Any(step => step.Example is not null || step.Values.Any(value => value.Origin == Syntax.Specifications.SpecificationValueOrigin.Case))))
        {
            return run;
        }

        var fixtures = origin.Steps.SelectMany(step => step.Values.Select(value =>
            $"{step.Role}{(step.Example is null ? string.Empty : $" {step.Example.Name}")}: {value.Property} = {ScreenplaySyntaxText.Expression(value.Value)} ({value.Origin.ToString().ToLowerInvariant()}{(value.OverriddenValue is null ? string.Empty : $", replaces {ScreenplaySyntaxText.Expression(value.OverriddenValue)}")})"));
        var provenance = $"Effective fixtures: {string.Join("; ", fixtures)}.";
        var prefix = origin.Case is { } row ? $"Case '{row.Name}' of '{origin.Authored.Name}': " : string.Empty;
        return run with { Failures = [.. run.Failures.Select(failure => $"{prefix}{failure} {provenance}")] };
    }

    static IEnumerable<SemanticSlice> AllSlices(ImmutableArray<SemanticFeature> features) =>
        features.SelectMany(feature => feature.Slices.Concat(AllSlices(feature.Features)));

    static SemanticExecutionResult EstablishWorld(
        SemanticExecutionPlan plan,
        SemanticSpecification specification,
        out SemanticWorld world)
    {
        var facts = specification.GivenEvents
            .Select(value => new SemanticFact(value.EventContract, value.EventSource?.Value ?? SemanticValue.Null, value.Values)
            {
                Context = value.EventSource is null ? null : new(value.EventSource),
                Route = plan.FormatFixtureRoute(value.Route)
            })
            .ToImmutableArray();
        var establishment = new SemanticEvaluator().EstablishSpecificationWorld(plan, facts);
        if (establishment is not SemanticAccepted accepted)
        {
            world = SemanticWorld.Empty;
            return establishment;
        }

        var readModels = accepted.World.ReadModels.ToList();
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

        try
        {
            world = SemanticWorld.Create(facts, [.. readModels]);
            return accepted;
        }
        catch (InvalidSemanticContract exception)
        {
            world = SemanticWorld.Empty;
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, exception.Message);
        }
    }

    // actionFacts is how many leading facts the action itself appended, in ESM v6; before v6 it is -1 and every fact is the action's.
    static ImmutableArray<string> Compare(
        SemanticExecutionPlan plan,
        SemanticSpecification expected,
        SemanticExecutionResult execution,
        int actionFacts = -1)
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

        // In ESM v6 the appended event is the action itself, like a command's input: 'then' events are what followed it.
        var following = actionFacts >= 0 && expected.WhenAppended is not null ? accepted.Facts[actionFacts..] : accepted.Facts;
        if (expected.WhenAppended is null || expected.ThenEvents.Length > 0)
        {
            CompareFacts(plan, expected.ThenEvents, following, failures, expected.ThenEventsInAnyOrder);
        }

        var commandFacts = actionFacts >= 0 ? accepted.Facts[..actionFacts] : accepted.Facts;
        var generatedIdentifier = expected.When is { } when && plan.Commands[when.Command].Properties.Any(property => property.IsIdentifier && property.IsGenerated);
        if (!generatedIdentifier && expected.When?.EventSource is { } commandSource && commandFacts.Any(fact => !SemanticValueRules.AreEqual(fact.Destination, commandSource.Value)))
        {
            failures.Add("Produced fact destination does not match the specification command event source.");
        }
        CompareReadModels(expected.ThenReadModels, accepted.World.ReadModels, failures, "read model");
        foreach (var absent in expected.ThenAbsentReadModels)
        {
            if (accepted.World.ReadModels.Any(instance => instance.ReadModel == absent.ReadModel &&
                SemanticValueRules.AreEqual(instance.Key, absent.Key)))
            {
                failures.Add($"Expected read model '{absent.ReadModel}' with key '{absent.Key}' to be absent.");
            }
        }
        CompareQueries(expected.ThenQueries, accepted.Queries, failures);
        CompareResponse(expected.ThenReturns, accepted.Response, expected.When is { } action ? plan.Commands[action.Command].Response : null, failures);
        return failures.ToImmutable();
    }

    static void CompareResponse(
        SemanticSpecificationResponse? expected,
        SemanticExecutionResponse? actual,
        SemanticCommandResponse? contract,
        ImmutableArray<string>.Builder failures)
    {
        switch (expected)
        {
            case null:
                return;
            case SemanticScalarSpecificationResponse scalar:
                if (actual is not SemanticScalarExecutionResponse response || !SemanticValueRules.AreEqual(scalar.Value, response.Value))
                {
                    failures.Add("Scalar response does not match the expected value.");
                }

                break;
            case SemanticRecordSpecificationResponse record:
                if (actual is not SemanticRecordExecutionResponse returned)
                {
                    failures.Add("Expected a record response.");
                    break;
                }

                var assertions = record.Fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
                var fields = returned.Fields.ToDictionary(field => field.Name, StringComparer.Ordinal);
                foreach (var field in ((SemanticRecordCommandResponse)contract!).Fields)
                {
                    if (assertions.TryGetValue(field.Name, out var assertion) &&
                        (!fields.TryGetValue(field.Name, out var value) || !SemanticValueRules.AreEqual(assertion.Value, value.Value)))
                    {
                        failures.Add($"Response field '{field.Name}' does not match the expected value.");
                    }
                }

                break;
            default:
                throw new InvalidSemanticContract("A specification response variant is unsupported.");
        }
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
        SemanticExecutionPlan plan,
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

        var routes = expected.Select(value => plan.FormatFixtureRoute(value.Route)).ToArray();
        if (inAnyOrder && plan.Model.SemanticVersion.IsAtLeast(SemanticVersion.V8))
        {
            // Augmenting paths reassign earlier wildcard matches instead of consuming an exact match greedily.
            var assignments = Enumerable.Repeat(-1, actual.Length).ToArray();
            for (var index = 0; index < expected.Length; index++)
            {
                if (!Assign(index, new bool[actual.Length]))
                {
                    failures.Add($"Fact at index {index} does not match the expected event contract and values.");
                }
            }

            return;

            bool Assign(int expectation, bool[] visited)
            {
                for (var fact = 0; fact < actual.Length; fact++)
                {
                    if (visited[fact] || !FactMatches(expected[expectation], actual[fact], routes[expectation])) continue;
                    visited[fact] = true;
                    if (assignments[fact] < 0 || Assign(assignments[fact], visited))
                    {
                        assignments[fact] = expectation;
                        return true;
                    }
                }

                return false;
            }
        }

        var remaining = actual.ToList();
        for (var index = 0; index < expected.Length; index++)
        {
            var matched = inAnyOrder ? remaining.FindIndex(fact => FactMatches(expected[index], fact, routes[index])) : index;
            if (matched < 0 || !FactMatches(expected[index], inAnyOrder ? remaining[matched] : actual[index], routes[index]))
            {
                failures.Add($"Fact at index {index} does not match the expected event contract and values.");
            }

            if (inAnyOrder && matched >= 0) remaining.RemoveAt(matched);
        }
    }

    static bool FactMatches(SemanticSpecificationEvent expected, SemanticFact actual, SemanticEventRoute? route) =>
        expected.EventContract == actual.EventContract && ValuesEqual(expected.Values, actual.Values) &&
        (route is null || route == actual.Route) && (!expected.Unrouted || actual.Route is null) &&
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

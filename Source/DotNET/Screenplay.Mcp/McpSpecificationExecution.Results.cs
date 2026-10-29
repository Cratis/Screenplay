// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Mcp;

internal static partial class McpSpecificationExecution
{
    static McpSpecificationResult Evaluate(McpSpecificationRun context, (SemanticId Id, string Address) assignment)
    {
        if (context.Admitted.Plan is not { } plan)
        {
            return new(assignment.Address, assignment.Id.ToString(), "unsupported", null, [], "Specification", string.Join("; ", context.Admitted.Issues.Select(issue => issue.Details)));
        }

        var run = SemanticSpecificationRunner.EnrichFailures(context.Compilation, context.Runner.RunWithCancellation(plan, assignment.Id, context.CancellationToken));
        var unsupported = run.Execution as SemanticUnsupported;
        var outcome = unsupported is not null ? "unsupported" : Outcome(run.Passed, false);
        var failures = Failures(plan, run);
        return new(assignment.Address, assignment.Id.ToString(), outcome, run.Execution.Kind.ToString(), [.. failures], unsupported?.Capability.ToString(), unsupported?.Details);
    }

    static IEnumerable<string> Failures(SemanticExecutionPlan plan, SemanticSpecificationRun run)
    {
        if (run.Passed || run.Execution is not SemanticAccepted accepted) return run.Failures;
        var expected = plan.Specifications[run.Specification];
        return run.Failures.Concat(Comparison(plan, expected, accepted));
    }

    static IEnumerable<string> Comparison(SemanticExecutionPlan plan, SemanticSpecification expected, SemanticAccepted accepted) =>
        [
            $"Expected facts: {string.Join("; ", expected.ThenEvents.Select(fact => DescribeFact(plan, fact.EventContract, fact.Values)))}",
            $"Actual facts: {string.Join("; ", accepted.Facts.Select(fact => DescribeFact(plan, fact.EventContract, fact.Values)))}",
            $"Expected read models: {string.Join("; ", expected.ThenReadModels.Select(state => DescribeState(plan, state.ReadModel, state.Key, state.Values)))}",
            $"Actual read models: {string.Join("; ", accepted.World.ReadModels.Select(state => DescribeState(plan, state.ReadModel, state.Key, state.Values)))}",
            $"Expected response: {DescribeResponse(expected.ThenReturns)}",
            $"Actual response: {DescribeResponse(accepted.Response)}",
            $"Expected query results: {string.Join("; ", expected.ThenQueries.Select(query => DescribeQuery(plan, query.Query, query.Key, query.Results.Select(state => DescribeState(plan, state.ReadModel, state.Key, state.Values)))))}",
            $"Actual query results: {string.Join("; ", accepted.Queries.Select(query => DescribeQuery(plan, query.Query, query.Key, query.Results.Select(state => DescribeState(plan, state.ReadModel, state.Key, state.Values)))))}"
        ];
}

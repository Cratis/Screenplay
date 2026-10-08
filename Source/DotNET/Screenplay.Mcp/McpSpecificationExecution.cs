// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed record McpSpecificationResult(string Address, string SemanticId, string Outcome, string? ExecutionOutcome, string[] Failures, string? Capability, string? Reason);
sealed record McpSpecificationReport(string SourceRevision, string Outcome, int Discovered, int Selected, int Executed, int Passed, int Failed, int Unsupported, Diagnostic[] Diagnostics, SemanticPlanIssue[] PlanIssues, McpSpecificationResult[] Results);

static class McpSpecificationExecution
{
    internal static McpSpecificationReport Run(ScreenplayWorkspace workspace, string? specification = null, string? scope = null)
    {
        var source = McpWorkspaceAnalysis.For(workspace).Source;
        var revision = source.SourceRevision;
        if (!workspace.Compilation.Success)
        {
            return new(revision, "unbound", source.Index.Declarations.Count(declaration => declaration.Kind == "Specification"), 0, 0, 0, 0, 0, [.. workspace.Compilation.Diagnostics], [], []);
        }

        var compilation = workspace.Compilation.Value!;
        var declarations = workspace.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Kind == SemanticKind.Specification)
            .Select(assignment => (assignment.Id, Address: Address(assignment.Address)))
            .OrderBy(assignment => assignment.Address, StringComparer.Ordinal).ToArray();
        var selected = declarations.Where(assignment =>
            (specification is null || assignment.Address == specification || assignment.Id.ToString() == specification) &&
            (scope is null || assignment.Address.StartsWith($"{scope}.", StringComparison.Ordinal))).ToArray();
        if ((specification is not null || scope is not null) && selected.Length == 0)
        {
            throw new McpFailure("UnknownSpecificationSelection: no specification matches the exact address, semantic id or scope.", -32602);
        }

        var admitted = SemanticExecutionPlan.Compile(compilation.Model);
        var runner = new SemanticSpecificationRunner();
        var results = selected.Select(assignment =>
        {
            if (admitted.Plan is not { } plan)
            {
                return new McpSpecificationResult(assignment.Address, assignment.Id.ToString(), "unsupported", null, [], "Specification", string.Join("; ", admitted.Issues.Select(issue => issue.Details)));
            }

            var run = runner.Run(plan, assignment.Id);
            var unsupported = run.Execution as SemanticUnsupported;
            var outcome = unsupported is not null ? "unsupported" : Outcome(run.Passed, false);
            var failures = run.Failures.AsEnumerable();
            if (!run.Passed && unsupported is null && run.Execution is SemanticAccepted accepted)
            {
                var expected = plan.Specifications[assignment.Id];
                failures = failures.Append($"Expected facts: {string.Join("; ", expected.ThenEvents.Select(fact => DescribeFact(plan, fact.EventContract, fact.Values)))}")
                    .Append($"Actual facts: {string.Join("; ", accepted.Facts.Select(fact => DescribeFact(plan, fact.EventContract, fact.Values)))}")
                    .Append($"Expected read models: {string.Join("; ", expected.ThenReadModels.Select(state => DescribeState(plan, state.ReadModel, state.Key, state.Values)))}")
                    .Append($"Actual read models: {string.Join("; ", accepted.World.ReadModels.Select(state => DescribeState(plan, state.ReadModel, state.Key, state.Values)))}")
                    .Append($"Expected response: {DescribeResponse(expected.ThenReturns)}")
                    .Append($"Actual response: {DescribeResponse(accepted.Response)}")
                    .Append($"Expected query results: {string.Join("; ", expected.ThenQueries.Select(query => DescribeQuery(plan, query.Query, query.Key, query.Results.Select(state => DescribeState(plan, state.ReadModel, state.Key, state.Values)))))}")
                    .Append($"Actual query results: {string.Join("; ", accepted.Queries.Select(query => DescribeQuery(plan, query.Query, query.Key, query.Results.Select(state => DescribeState(plan, state.ReadModel, state.Key, state.Values)))))}");
            }

            return new McpSpecificationResult(assignment.Address, assignment.Id.ToString(), outcome, run.Execution.Kind.ToString(), [.. failures], unsupported?.Capability.ToString(), unsupported?.Details);
        }).ToArray();
        var passed = results.Count(result => result.Outcome == "passed");
        var failed = results.Count(result => result.Outcome == "failed");
        var unsupportedCount = results.Count(result => result.Outcome == "unsupported");
        var outcome = Outcome(failed == 0, unsupportedCount > 0 || admitted.Plan is null);

        return new(revision, outcome, declarations.Length, selected.Length, passed + failed, passed, failed, unsupportedCount, [.. workspace.Compilation.Diagnostics], [.. admitted.Issues], results);
    }

    static string DescribeFact(SemanticExecutionPlan plan, SemanticId contract, IEnumerable<SemanticPropertyValue> values)
    {
        var declaration = plan.Events[contract];
        var properties = values.Select(value => $"{declaration.Properties.FirstOrDefault(property => property.Id == value.TargetProperty)?.Name ?? value.TargetProperty.ToString()} = {value.Value}");

        return $"{declaration.Name} ({string.Join(", ", properties)})";
    }

    static string DescribeState(SemanticExecutionPlan plan, SemanticId model, SemanticValue key, IEnumerable<SemanticPropertyValue> values)
    {
        var declaration = plan.ReadModels[model];
        var properties = values.Select(value => $"{declaration.Properties.FirstOrDefault(property => property.Id == value.TargetProperty)?.Name ?? value.TargetProperty.ToString()} = {value.Value}");

        return $"{declaration.Name} key {key} ({string.Join(", ", properties)})";
    }

    static string DescribeResponse(object? response) => response switch
    {
        SemanticScalarSpecificationResponse scalar => scalar.Value.ToString(),
        SemanticScalarExecutionResponse scalar => scalar.Value.ToString(),
        SemanticRecordSpecificationResponse record => string.Join(", ", record.Fields.Select(field => $"{field.Name} = {field.Value}")),
        SemanticRecordExecutionResponse record => string.Join(", ", record.Fields.Select(field => $"{field.Name} = {field.Value}")),
        _ => "none"
    };

    static string DescribeQuery(SemanticExecutionPlan plan, SemanticId query, SemanticValue key, IEnumerable<string> rows) =>
        $"{plan.Queries[query].Name} key {key} [{string.Join("; ", rows)}]";

    static string Outcome(bool passed, bool unsupported) => (passed, unsupported) switch
    {
        (_, true) => "unsupported",
        (true, false) => "passed",
        _ => "failed"
    };

    static string Address(SemanticAddress address) => string.Join('.', address.Parts.Where(part => part.Kind != SemanticAddressPartKind.Application).Select(part => part.Key));
}

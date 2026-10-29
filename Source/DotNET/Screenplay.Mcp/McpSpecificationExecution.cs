// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed record McpSpecificationResult(string Address, string SemanticId, string Outcome, string? ExecutionOutcome, string[] Failures, string? Capability, string? Reason);
sealed record McpSpecificationReport(string SourceRevision, string Outcome, int Discovered, int Selected, int Executed, int Passed, int Failed, int Unsupported, Diagnostic[] Diagnostics, SemanticPlanIssue[] PlanIssues, McpSpecificationResult[] Results);

sealed record McpSpecificationSelection(string? Specification = null, string? Scope = null, IReadOnlySet<DocumentId>? Documents = null);
sealed record McpRuleExecution(ISemanticBodyExecutionProviderFactory? Factory, CancellationToken CancellationToken);
sealed record McpSpecificationRun(SemanticCompilation Compilation, SemanticExecutionPlanCompilation Admitted, SemanticSpecificationRunner Runner, CancellationToken CancellationToken);

internal static partial class McpSpecificationExecution
{
    internal static McpSpecificationReport Run(ScreenplayWorkspace workspace, string? specification = null, string? scope = null) =>
        Run(workspace, new McpSpecificationSelection(specification, scope), new McpRuleExecution(null, default));

    internal static McpSpecificationReport Run(ScreenplayWorkspace workspace, McpSpecificationSelection selection, McpRuleExecution execution)
    {
        var (specification, scope, documents) = selection;
        var source = McpWorkspaceAnalysis.For(workspace).Source;
        var revision = source.SourceRevision;
        if (!workspace.Compilation.Success)
        {
            return new(revision, "unbound", source.Index.Declarations.Count(declaration => declaration.Kind == "Specification"), 0, 0, 0, 0, 0, [.. workspace.Compilation.Diagnostics], [], []);
        }

        var compilation = workspace.Compilation.Value!;
        var declarations = workspace.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Kind == SemanticKind.Specification)
            .Where(assignment => documents is null || compilation.SourceMap.Entries.Any(entry => entry.SemanticId == assignment.Id && entry.Role == SemanticSourceMapRole.Declaration && documents.Contains(entry.Span.Document)))
            .Select(assignment => (assignment.Id, Address: Address(assignment.Address)))
            .OrderBy(assignment => assignment.Address, StringComparer.Ordinal).ToArray();
        var selected = declarations.Where(assignment =>
            (specification is null || assignment.Address == specification || assignment.Id.ToString() == specification || TableAddress(compilation, assignment.Id, assignment.Address) == specification) &&
            (scope is null || assignment.Address == scope || assignment.Address.StartsWith($"{scope}.", StringComparison.Ordinal) || TableAddress(compilation, assignment.Id, assignment.Address) == scope)).ToArray();
        if ((specification is not null || scope is not null) && selected.Length == 0)
        {
            throw new McpFailure("UnknownSpecificationSelection: no specification matches the exact address, semantic id or scope.", -32602);
        }

        var admitted = SemanticExecutionPlan.CompileForSpecifications(compilation.Model);
        using var bodyRun = new SemanticBodyExecutionRun(execution.Factory, compilation.Model.Application, execution.Factory is null ? [] : McpRuleBodies.Resolve(workspace), execution.CancellationToken);
        var context = new McpSpecificationRun(compilation, admitted, new SemanticSpecificationRunner(bodyRun.Evaluator), execution.CancellationToken);
        var results = selected.Select(assignment => Evaluate(context, assignment)).ToArray();
        var passed = results.Count(result => result.Outcome == "passed");
        var failed = results.Count(result => result.Outcome == "failed");
        var unsupportedCount = results.Count(result => result.Outcome == "unsupported");
        var outcome = Outcome(failed == 0, unsupportedCount > 0 || admitted.Plan is null);

        return new(revision, outcome, declarations.Length, selected.Length, passed + failed, passed, failed, unsupportedCount, [.. workspace.Compilation.Diagnostics], [.. admitted.Issues, .. admitted.UnselectedIssues], results);
    }

    static string? TableAddress(SemanticCompilation compilation, SemanticId id, string address) =>
        compilation.SpecificationOrigins.TryGetValue(id, out var origin) && origin.Case is not null
            ? address[..^origin.Effective.Name.Length] + origin.Authored.Name : null;

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
        (false, _) => "failed",
        (true, true) => "unsupported",
        _ => "passed"
    };

    static string Address(SemanticAddress address) => string.Join('.', address.Parts.Where(part => part.Kind != SemanticAddressPartKind.Application).Select(part => part.Key));
}

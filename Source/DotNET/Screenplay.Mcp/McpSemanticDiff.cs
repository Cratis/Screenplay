// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// MCP owns revision checks, the frozen JSON projection and paging, not comparison semantics.
static class McpSemanticDiff
{
    internal static object Read(IMcpProposal proposal, JsonElement arguments) => Read(proposal.Before, proposal.Workspace, arguments, proposal.Workspace.Revision.ToString(), false);

    internal static object Compare(ScreenplayWorkspace before, ScreenplayWorkspace after, JsonElement arguments) =>
        Read(before, after, arguments, $"comparison:{Hash($"{before.Revision}:{after.Revision}")}", true);

    static object Read(ScreenplayWorkspace baseline, ScreenplayWorkspace candidate, JsonElement arguments, string revision, bool revisions)
    {
        var expected = McpJson.OptionalString(arguments, "expectedSourceRevision");
        if (expected is not null && expected != revision)
        {
            throw new McpFailure(revisions ? "StaleRevision: semantic-diff pages must identify both snapshot revisions." : "StaleRevision: semantic-diff pages must identify the proposal revision.") { FailureKind = revisions ? "StaleRevision" : "RequestFailed" };
        }
        if (McpJson.Integer(arguments, "offset", 0, 0, int.MaxValue) > 0 && expected is null)
        {
            throw new McpFailure("'expectedSourceRevision' is required for continuation.", -32602);
        }

        var before = McpWorkspaceAnalysis.For(baseline);
        var after = McpWorkspaceAnalysis.For(candidate);
        var difference = StructuralComparison.Compare(baseline, candidate, before.Source, before.Syntax, after.Source, after.Syntax);
        return new
        {
            sourceRevision = revision,
            beforeRevision = baseline.Revision.ToString(),
            afterRevision = candidate.Revision.ToString(),
            complete = difference.Complete,
            hasSemanticChange = difference.HasSemanticChange,
            comparisonLevel = "authoring-structure",
            executableBeforeAvailable = baseline.Compilation.Success,
            executableAfterAvailable = candidate.Compilation.Success,
            sections = difference.Sections.Select(section => new
            {
                section = section.Section,
                complete = section.Complete,
                unavailable = section.Gaps.Select(gap => gap.Statement).ToArray()
            }).ToArray(),
            limits = revisions ? difference.Limits : [.. difference.Limits, "No revision-to-revision comparison."],
            page = McpPaging.BoundedSourcePage(difference.Changes.Select(Project).Cast<object>(), arguments, revision)
        };
    }

    static Change Project(StructuralComparison.Change change) => new(
        change.Section,
        change.ChangeKind,
        change.SemanticId,
        change.Kind,
        change.BeforeAddress,
        change.AfterAddress,
        change.Member,
        change.BeforeHash,
        change.AfterHash,
        change.BeforeType,
        change.AfterType,
        change.ContractBreaking,
        change.GenerationCovered,
        change.BeforeGeneration,
        change.AfterGeneration,
        change.BeforeDocuments?.Select(document => new DocumentLocation(document.DocumentId, document.Path)).ToArray(),
        change.AfterDocuments?.Select(document => new DocumentLocation(document.DocumentId, document.Path)).ToArray(),
        change.Snapshot,
        change.DependantAddress,
        change.Role,
        change.Resolution,
        change.EventContractId,
        change.MoveKind,
        change.BeforeOwner,
        change.AfterOwner);

    static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    sealed record DocumentLocation(string DocumentId, string Path);

    sealed record Change(string Section, string ChangeKind, string? SemanticId, string Kind, string? BeforeAddress, string? AfterAddress, string? Member = null,
        string? BeforeHash = null, string? AfterHash = null, string? BeforeType = null, string? AfterType = null, bool? ContractBreaking = null, bool? GenerationCovered = null,
        uint? BeforeGeneration = null, uint? AfterGeneration = null, DocumentLocation[]? BeforeDocuments = null, DocumentLocation[]? AfterDocuments = null,
        string? Snapshot = null, string? DependantAddress = null, string? Role = null, string? Resolution = null, string? EventContractId = null,
        string? MoveKind = null, string? BeforeOwner = null, string? AfterOwner = null);
}

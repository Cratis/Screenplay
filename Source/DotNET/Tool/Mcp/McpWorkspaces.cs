// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

sealed class McpWorkspaces(McpRoot root)
{
    readonly Dictionary<string, McpProposal> _proposals = new(StringComparer.Ordinal);
    ScreenplayWorkspace? _workspace;

    internal object Open(JsonElement arguments)
    {
        var serialized = McpJson.OptionalString(arguments, "workspaceJson");
        var name = McpJson.OptionalString(arguments, "applicationName") ?? root.ApplicationName;
        var candidate = serialized is null
            ? ScreenplayWorkspace.Create(name, root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create(name)))
            : McpWorkspaceTransport.Restore(serialized);
        root.Verify(candidate);
        var result = McpWorkspaceTransport.Describe(candidate);
        _workspace = candidate;
        _proposals.Clear();
        return McpJson.ToolResult(result);
    }

    internal object Propose(JsonElement arguments, bool expand)
    {
        var workspace = Current();
        root.Verify(workspace);
        var request = new WorkspaceTransactionRequest
        {
            ExpectedRevision = WorkspaceRevision.Parse(McpJson.RequiredString(arguments, "expectedRevision")),
            ExpectedCatalogRevision = CatalogRevision.Parse(McpJson.RequiredString(arguments, "expectedCatalogRevision")),
            Operations = expand ? McpLayout.Expand(workspace) : McpWorkspaceOperations.Read(arguments)
        };
        var transaction = workspace.Propose(request);
        if (!transaction.Success)
        {
            return McpJson.ToolResult(new { success = false, transaction.Conflicts, transaction.Diagnostics }, true);
        }

        var candidate = transaction.Workspace!;
        McpRoot.CheckDocuments(candidate.Documents);
        if (!candidate.Compilation.Success || !new McpSnapshot(candidate.Documents).Compilation.Success)
        {
            throw new McpFailure("Candidate compilation failed; no proposal was created.");
        }

        if (_proposals.Count >= 16)
        {
            throw new McpFailure("The session already holds 16 proposals. Reopen the workspace to discard them.");
        }

        var id = Guid.NewGuid().ToString("N");
        var result = new
        {
            success = true,
            proposalId = id,
            before = McpWorkspaceTransport.Describe(workspace),
            after = McpWorkspaceTransport.Describe(candidate),
            changes = transaction.WritePlan!.Entries.Select(entry => new
            {
                kind = entry.Kind,
                document = entry.Document.ToString(),
                before = DescribeDocument(entry.Before),
                after = DescribeDocument(entry.After)
            }),
            conflicts = transaction.Conflicts
        };
        var response = McpJson.ToolResult(result);
        _proposals.Add(id, new(workspace, transaction));
        return response;
    }

    internal object Apply(JsonElement arguments)
    {
        var workspace = Current();
        var id = McpJson.RequiredString(arguments, "proposalId");
        if (!_proposals.TryGetValue(id, out var proposal))
        {
            throw new McpFailure("UnknownProposal: only a proposal created by this connection can be applied.");
        }

        if (McpJson.RequiredString(arguments, "expectedRevision") != workspace.Revision.ToString() ||
            McpJson.RequiredString(arguments, "expectedCatalogRevision") != workspace.IdentityCatalog.Revision.ToString() ||
            proposal.Before.Revision != workspace.Revision || proposal.Before.IdentityCatalog.Revision != workspace.IdentityCatalog.Revision)
        {
            throw new McpFailure("StaleRevision: workspace or catalog revision no longer matches the proposal.");
        }

        var result = new McpDisk(root).Apply(proposal);
        if (result.Success)
        {
            _workspace = proposal.Transaction.Workspace;
            _proposals.Clear();
        }

        return McpJson.ToolResult(new { result.Success, result.Status, result.Recovery, result.PlannedChanges, result.InstalledDocuments, workspace = McpWorkspaceTransport.Describe(Current()) }, !result.Success);
    }

    static object? DescribeDocument(WorkspaceDocument? document) => document is null ? null : new
    {
        path = document.Path.Value,
        document.Text,
        bytes = Convert.ToBase64String(document.Bytes.AsSpan())
    };

    ScreenplayWorkspace Current() => _workspace ?? throw new McpFailure("Open a workspace first.");
}

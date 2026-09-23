// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces(McpRoot root)
{
    readonly Dictionary<string, IMcpProposal> _proposals = new(StringComparer.Ordinal);
    readonly ConditionalWeakTable<IMcpProposal, McpStatePlan> _statePlans = [];
    ScreenplayWorkspace? _workspace;
    byte[]? _stateBytes;

    internal object Open(JsonElement arguments)
    {
        var serialized = McpJson.OptionalString(arguments, "workspaceJson");
        McpRecoveryJournal.RefusePending(root);
        var persisted = new McpManagedFiles(root).Read(McpState.FileName);
        var state = persisted is null ? null : McpState.Deserialize(persisted);
        var name = McpJson.OptionalString(arguments, "applicationName") ?? state?.ApplicationName ?? root.ApplicationName;
        if (state is not null && name != state.ApplicationName)
        {
            throw new McpFailure("IdentityStateConflict: applicationName differs from the persisted application. Reopen without overriding its name.");
        }

        var candidate = serialized is null ? state?.Open(root) ?? OpenFromDisk(name) : McpWorkspaceTransport.Restore(serialized);
        if (persisted is not null && !McpManagedFiles.Equal(persisted, McpState.Serialize(candidate)))
        {
            throw new McpFailure("IdentityImportConflict: workspaceJson cannot replace a different persisted identity catalog or document mapping.");
        }

        root.Verify(candidate);
        new McpManagedFiles(root).Verify(McpState.FileName, persisted);
        McpRecoveryJournal.RefusePending(root);
        _ = McpWorkspaceTransport.ExportBytes(candidate);
        var result = McpJson.ToolResult(McpWorkspaceTransport.Describe(candidate, McpJson.Boolean(arguments, "includeContent")));
        _workspace = candidate;
        _stateBytes = persisted;
        _proposals.Clear();
        _statePlans.Clear();
        return result;
    }

    internal object Propose(JsonElement arguments, bool expand)
    {
        var workspace = Current();
        var expectedRevision = WorkspaceRevision.Parse(McpJson.RequiredString(arguments, "expectedRevision"));
        var expectedCatalogRevision = CatalogRevision.Parse(McpJson.RequiredString(arguments, "expectedCatalogRevision"));
        if (expectedRevision != workspace.Revision || expectedCatalogRevision != workspace.IdentityCatalog.Revision)
        {
            var stale = workspace.Propose(new() { ExpectedRevision = expectedRevision, ExpectedCatalogRevision = expectedCatalogRevision });
            return McpJson.ToolResult(new { success = false, stale.Conflicts, stale.Diagnostics }, true);
        }

        root.Verify(workspace);
        var request = new WorkspaceTransactionRequest
        {
            ExpectedRevision = expectedRevision,
            ExpectedCatalogRevision = expectedCatalogRevision,
            Operations = expand ? McpLayout.Expand(workspace, McpJson.OptionalString(arguments, "layout") ?? "slice") : McpWorkspaceOperations.Read(arguments),
            SemanticRenames = McpIdentityChanges.SemanticRenames(arguments),
            EventRenames = McpIdentityChanges.EventRenames(arguments),
            RetiredSemanticAddresses = McpIdentityChanges.RetiredSemanticAddresses(arguments),
            RetiredEventAddresses = McpIdentityChanges.RetiredEventAddresses(arguments)
        };
        var transaction = workspace.Propose(request);
        if (!transaction.Success)
        {
            return McpJson.ToolResult(new { success = false, transaction.Conflicts, transaction.Diagnostics }, true);
        }

        return Store(new McpProposal(workspace, transaction), arguments);
    }

    internal object Apply(JsonElement arguments)
    {
        var workspace = CheckedCurrent(arguments);
        var proposal = Proposal(arguments);
        if (McpJson.RequiredString(arguments, "expectedCatalogRevision") != workspace.IdentityCatalog.Revision.ToString() ||
            proposal.Before.Revision != workspace.Revision || proposal.Before.IdentityCatalog.Revision != workspace.IdentityCatalog.Revision)
        {
            throw new McpFailure("StaleRevision: workspace or catalog revision no longer matches the proposal.");
        }

        var statePlan = StatePlan(proposal);
        var includeContent = McpJson.Boolean(arguments, "includeContent");
        var beforeDescription = JsonSerializer.SerializeToElement(McpWorkspaceTransport.Describe(workspace, includeContent), McpJson.Options);
        var afterDescription = JsonSerializer.SerializeToElement(McpWorkspaceTransport.Describe(proposal.Workspace, includeContent), McpJson.Options);
        var result = new McpDisk(root).Apply(proposal, statePlan);
        if (result.Success)
        {
            _workspace = proposal.Workspace;
            _stateBytes = statePlan.After;
            _proposals.Clear();
            _statePlans.Clear();
        }

        var response = new
        {
            result.Success,
            result.Status,
            result.Recovery,
            result.PlannedChanges,
            result.InstalledDocuments,
            validation = proposal.Validation,
            referencePolicy = proposal is McpAuthoringProposal authored ? authored.ReferencePolicy.ToString() : null,
            workspace = result.Success ? afterDescription : beforeDescription
        };
        return McpJson.ToolResult(response, !result.Success, enforceBudget: false);
    }

    object Store(IMcpProposal proposal, JsonElement arguments)
    {
        _ = Current();
        var candidate = proposal.Workspace;
        McpRoot.CheckDocuments(candidate.Documents);
        foreach (var document in candidate.Documents)
        {
            _ = root.PathFor(document.Path);
        }

        if (!proposal.Accepted || !McpWorkspaceAnalysis.For(candidate).Source.Compilation.Success)
        {
            throw new McpFailure("Candidate compilation failed; no proposal was created.");
        }

        if (_proposals.Count >= 16)
        {
            throw new McpFailure("The session already holds 16 proposals. Discard a proposal or reopen the workspace.");
        }

        _ = McpWorkspaceTransport.ExportBytes(candidate);
        var includeContent = McpJson.Boolean(arguments, "includeContent");
        var id = Guid.NewGuid().ToString("N");
        var statePlan = new McpStatePlan(_stateBytes, McpState.Serialize(candidate));
        var result = new
        {
            success = true,
            proposalId = id,
            validation = proposal.Validation,
            referencePolicy = proposal is McpAuthoringProposal policy ? policy.ReferencePolicy.ToString() : null,
            before = McpWorkspaceTransport.Describe(proposal.Before, includeContent),
            after = McpWorkspaceTransport.Describe(candidate, includeContent),
            changeCount = proposal.WritePlan.Entries.Length,
            stateChange = statePlan.Describe(),
            authoringDiagnosticCount = proposal is McpAuthoringProposal authored ? authored.Result.AuthoringDiagnostics.Length : 0,
            canonicalizedSource = proposal is McpAuthoringProposal formatted && formatted.Result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization),
            changes = includeContent ? proposal.WritePlan.Entries.Select(DescribeChange) : null,
            review = "Use read-proposal to inspect the complete plan and exact before/after bytes before apply."
        };
        var response = McpJson.ToolResult(result);
        _proposals.Add(id, proposal);
        _statePlans.Add(proposal, statePlan);
        return response;
    }

    IMcpProposal Proposal(JsonElement arguments)
    {
        var id = McpJson.RequiredString(arguments, "proposalId");
        return _proposals.TryGetValue(id, out var proposal) ? proposal : throw new McpFailure("UnknownProposal: only an outstanding proposal from this connection can be used.");
    }

    ScreenplayWorkspace CheckedCurrent(JsonElement arguments)
    {
        var workspace = Current();
        if (McpJson.RequiredString(arguments, "expectedRevision") != workspace.Revision.ToString())
        {
            throw new McpFailure("StaleRevision: reopen the workspace before continuing.");
        }

        root.Verify(workspace);
        return workspace;
    }

    ScreenplayWorkspace OpenFromDisk(string name)
    {
        var documents = root.Read(allowEmpty: true);
        var identity = ApplicationIdentity.Create(name);
        return documents.IsEmpty
            ? ScreenplayWorkspace.CreateEmpty(identity, name)
            : ScreenplayWorkspace.Create(name, documents, SemanticIdentityCatalog.Empty(identity));
    }

    ScreenplayWorkspace Current()
    {
        McpRecoveryJournal.RefusePending(root);
        var workspace = _workspace ?? throw new McpFailure("Open a workspace first.");
        new McpManagedFiles(root).Verify(McpState.FileName, _stateBytes);
        return workspace;
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpWorkspaces
{
    readonly Dictionary<string, IMcpProposal> _proposals = new(StringComparer.Ordinal);
    readonly ConditionalWeakTable<IMcpProposal, McpStatePlan> _statePlans = [];
    readonly ConditionalWeakTable<IMcpProposal, McpRepairEvidence> _repairEvidence = [];

    readonly bool _staticRoot;
    McpRoot? _root;
    ScreenplayWorkspace? _workspace;
    byte[]? _stateBytes;

    internal McpWorkspaces(McpRoot? root = null)
    {
        _staticRoot = root is not null;
        _root = root;
    }

    // Deterministic specs can interrupt between validation and retention, or staging and installation.
    internal Action? BeforeStore { get; set; }
    internal Action? BeforeInstall { get; set; }

    // The client roots a host advertises through the MCP roots capability; empty until the host answers.
    internal ImmutableArray<string> ClientRoots { get; set; } = [];

    // The working directory a dynamic server falls back to when it binds no root itself; specs replace it.
    internal string? CurrentDirectoryHint { get; set; }

    // Whether the server was started without a fixed root and chooses one per workspace.
    internal bool DynamicRoot => !_staticRoot;

    // The path of a root bound from a single client root, null when unbound or chosen by path.
    internal string? ClientDerivedRootPath { get; private set; }

    // The bound root. A dynamic server binds its default root the first time any tool needs one, so no tool
    // has to be preceded by open-workspace.
    McpRoot Root => _root ?? BindDefaultRoot();

    internal McpRoot ReadRoot() => Root;

    internal object Open(JsonElement arguments)
    {
        // A dynamic server chooses its root here: an explicit path wins, then a single client root,
        // then the working directory when it already holds Screenplay source. A static root stays bound.
        var requestedPath = McpJson.OptionalString(arguments, "path");
        if (requestedPath is not null)
        {
            var requestedRoot = new McpRoot(Path.GetFullPath(requestedPath, CurrentDirectoryHint ?? Environment.CurrentDirectory));
            if (_staticRoot)
            {
                if (!McpDirectoryIdentity.Same(Root, requestedRoot))
                {
                    throw new McpFailure("A fixed-root server cannot switch to another application root.") { FailureKind = "RootChangeRefused" };
                }

                // A proven alias is admitted, but never replaces the originally approved root object or spelling.
            }
            else
            {
                ClientDerivedRootPath = null;
                BindRoot(requestedRoot);
            }
        }
        else if (_root is null)
        {
            BindDefaultRoot();
        }

        var serialized = McpJson.OptionalString(arguments, "workspaceJson");
        McpRecoveryJournal.RefusePending(Root);
        var persisted = new McpManagedFiles(Root).Read(McpState.FileName);
        var state = persisted is null ? null : McpState.Deserialize(persisted);
        var name = McpJson.OptionalString(arguments, "applicationName") ?? state?.ApplicationName ?? Root.ApplicationName;
        if (state is not null && name != state.ApplicationName)
        {
            throw new McpFailure("IdentityStateConflict: applicationName differs from the persisted application. Reopen without overriding its name.");
        }

        var candidate = serialized is null
            ? state?.Open(Root) ?? OpenFromDisk(name)
            : McpAttachmentContents.Refresh(Root, McpWorkspaceTransport.Restore(serialized));
        if (persisted is not null && !McpManagedFiles.Equal(persisted, McpState.Serialize(candidate)))
        {
            throw new McpFailure("IdentityImportConflict: workspaceJson cannot replace a different persisted identity catalog or document mapping.");
        }

        Root.Verify(candidate);
        new McpManagedFiles(Root).Verify(McpState.FileName, persisted);
        McpRecoveryJournal.RefusePending(Root);
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
            return McpJson.ToolResult(new { success = false, failureKind = "StaleRevision", stale.Conflicts, stale.Diagnostics }, true);
        }

        Root.Verify(workspace);
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
            return McpJson.ToolResult(new { success = false, failureKind = "ProposalRejected", transaction.Conflicts, transaction.Diagnostics }, true);
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
            throw new McpFailure("StaleRevision: workspace or catalog revision no longer matches the proposal.") { FailureKind = "StaleRevision" };
        }

        var statePlan = StatePlan(proposal);
        var includeContent = McpJson.Boolean(arguments, "includeContent");
        var beforeDescription = JsonSerializer.SerializeToElement(McpWorkspaceTransport.Describe(workspace, includeContent), McpJson.Options);
        var afterDescription = JsonSerializer.SerializeToElement(McpWorkspaceTransport.Describe(proposal.Workspace, includeContent), McpJson.Options);
        _repairEvidence.TryGetValue(proposal, out var evidence);
        var result = new McpDisk(Root).Apply(
            proposal,
            statePlan,
            () =>
            {
                BeforeInstall?.Invoke();
                evidence?.Verify(Root, proposal);
            },
            evidence?.OperationId);
        if (result.Success)
        {
            _workspace = evidence is null ? McpAttachmentContents.Refresh(Root, proposal.Workspace) : proposal.Workspace;
            _stateBytes = statePlan.After;
            _proposals.Clear();
            _statePlans.Clear();
        }

        var response = new
        {
            result.Success,
            result.Status,
            failureKind = result.FailureKind,
            result.Recovery,
            result.PlannedChanges,
            result.InstalledDocuments,
            validation = proposal.Validation,
            referencePolicy = proposal is McpAuthoringProposal authored ? authored.ReferencePolicy.ToString() : null,
            workspace = result.Success ? afterDescription : beforeDescription
        };
        return McpJson.ToolResult(response, !result.Success, enforceBudget: false);
    }

    // Drops a binding that came from client roots after the host reports the roots changed; an explicit
    // path the caller chose survives.
    internal void UnbindClientRoot(string directoryPath)
    {
        if (_staticRoot || _root is null || !string.Equals(_root.DirectoryPath, directoryPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _root = null;
        _workspace = null;
        _stateBytes = null;
        _proposals.Clear();
        _statePlans.Clear();
    }

    static McpRoot RootFromClientUri(string uri)
    {
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed) || !string.Equals(parsed.Scheme, "file", StringComparison.OrdinalIgnoreCase))
        {
            throw new McpFailure($"A client root must be a file URI: '{uri}'.");
        }

        return new McpRoot(Uri.UnescapeDataString(parsed.AbsolutePath));
    }

    // A shallow, bounded check: .play files at the top or one level down, or an existing identity-state folder.
    static bool LooksLikeScreenplayRoot(string directory)
    {
        if (Directory.Exists(Path.Combine(directory, ".screenplay")) || Directory.EnumerateFiles(directory, "*.play").Any())
        {
            return true;
        }

        foreach (var child in Directory.EnumerateDirectories(directory))
        {
            var name = Path.GetFileName(child);
            if (name.Equals(".git", StringComparison.Ordinal) || name.Equals(".ai-work", StringComparison.Ordinal) ||
                name.Equals("bin", StringComparison.OrdinalIgnoreCase) || name.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Directory.EnumerateFiles(child, "*.play").Any())
            {
                return true;
            }
        }

        return false;
    }

    object Store(IMcpProposal proposal, JsonElement arguments)
    {
        BeforeStore?.Invoke();
        var pinned = McpJson.Boolean(arguments, "pinRepairEvidence");
        var evidence = pinned ? McpRepairEvidence.Pin(Root, proposal) : null;
        if (!pinned) proposal = RefreshProposal(proposal);
        var candidate = proposal.Workspace;
        McpRoot.CheckDocuments(candidate.Documents);
        foreach (var document in candidate.Documents)
        {
            _ = Root.PathFor(document.Path);
        }

        // The rejection carries the exact diagnostics so a caller can correct the candidate instead of guessing.
        var analysis = McpWorkspaceAnalysis.For(candidate);
        if (!proposal.Accepted || !analysis.Source.Compilation.Success)
        {
            var issues = string.Join(" | ", analysis.Source.Compilation.Diagnostics.Concat(candidate.Compilation.Diagnostics)
                .Select(diagnostic => $"{diagnostic.Code} {diagnostic.Location.Line}:{diagnostic.Location.Column} {diagnostic.Message}")
                .Take(8));
            throw new McpFailure($"Candidate compilation failed; no proposal was created. {(issues.Length == 0 ? "No diagnostics were reported." : issues)}");
        }

        if (_proposals.Count >= 16)
        {
            throw new McpFailure("The session already holds 16 proposals. Discard a proposal or reopen the workspace.") { FailureKind = "LimitExceeded" };
        }

        _ = McpWorkspaceTransport.ExportBytes(candidate);
        var includeContent = McpJson.Boolean(arguments, "includeContent");
        var id = Guid.NewGuid().ToString("N");
        var statePlan = new McpStatePlan(_stateBytes, McpState.Serialize(candidate));
        var introducedErrors = McpIntroducedErrors.Between(proposal.Before, candidate);
        var result = new
        {
            success = true,
            proposalId = id,
            repairEvidence = evidence,
            validation = proposal.Validation,
            referencePolicy = proposal is McpAuthoringProposal policy ? policy.ReferencePolicy.ToString() : null,
            before = McpWorkspaceTransport.Describe(proposal.Before, includeContent),
            after = McpWorkspaceTransport.Describe(candidate, includeContent),
            changeCount = proposal.WritePlan.Entries.Length,
            stateChange = statePlan.Describe(),
            authoringDiagnosticCount = proposal is McpAuthoringProposal authored ? authored.Result.AuthoringDiagnostics.Length : 0,
            droppedCommentCount = WorkspaceDroppedComments.In(proposal.WritePlan).Length,
            canonicalizedSource = proposal is McpAuthoringProposal formatted && formatted.Result.AuthoringDiagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.AuthoringSourceNormalization),
            changes = includeContent ? proposal.WritePlan.Entries.Select(DescribeChange) : null,
            introducedExecutableErrors = introducedErrors,
            executableGuidance = introducedErrors.Count == 0 ? null : McpIntroducedErrors.Guidance,
            review = "Use read-proposal to inspect the complete plan, any dropped comments, and exact before/after bytes before apply."
        };
        var response = McpJson.ToolResult(result);
        _proposals.Add(id, proposal);
        _statePlans.Add(proposal, statePlan);
        if (evidence is not null) _repairEvidence.Add(proposal, evidence);
        return response;
    }

    void BindRoot(McpRoot candidate)
    {
        if (_root is not null && string.Equals(_root.DirectoryPath, candidate.DirectoryPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _root = candidate;
        _workspace = null;
        _stateBytes = null;
        _proposals.Clear();
        _statePlans.Clear();
    }

    McpRoot BindDefaultRoot()
    {
        var resolved = ResolveDefaultRoot();
        ClientDerivedRootPath = ClientRoots.Length == 1 ? resolved.DirectoryPath : null;
        BindRoot(resolved);
        return resolved;
    }

    McpRoot ResolveDefaultRoot()
    {
        if (ClientRoots.Length == 1)
        {
            return RootFromClientUri(ClientRoots[0]);
        }

        if (ClientRoots.Length > 1)
        {
            throw new McpFailure(
                $"The client offers {ClientRoots.Length} roots; pass open-workspace with a path to choose one: {string.Join(", ", ClientRoots)}");
        }

        var directory = CurrentDirectoryHint ?? Environment.CurrentDirectory;
        if (LooksLikeScreenplayRoot(directory))
        {
            return new McpRoot(directory);
        }

        throw new McpFailure(
            "No Screenplay root was given and the current directory holds no .play files. Pass open-workspace with a path to the folder holding the model.");
    }

    IMcpProposal Proposal(JsonElement arguments)
    {
        var id = McpJson.RequiredString(arguments, "proposalId");
        if (!_proposals.TryGetValue(id, out var proposal))
        {
            throw new McpFailure("UnknownProposal: only an outstanding proposal from this connection can be used.") { FailureKind = "UnknownProposal" };
        }

        var expectedEvidence = McpRepairEvidence.Expected(arguments);
        if (_repairEvidence.TryGetValue(proposal, out var evidence))
        {
            if (expectedEvidence is not null && expectedEvidence != evidence.BeforeRevision)
            {
                throw new McpFailure("The supplied revision does not identify the retained repair evidence.") { FailureKind = "RepairEvidenceDrift" };
            }

            Root.Verify(proposal.Before);
            evidence.Verify(Root, proposal);
            return proposal;
        }

        if (expectedEvidence is not null)
        {
            throw new McpFailure("The proposal has no pinned repair evidence.", -32602);
        }

        var refreshed = RefreshProposal(proposal);
        if (!ReferenceEquals(proposal, refreshed))
        {
            if (_statePlans.TryGetValue(proposal, out var plan))
            {
                _statePlans.Add(refreshed, plan);
            }

            _proposals[id] = refreshed;
        }

        return refreshed;
    }

    IMcpProposal RefreshProposal(IMcpProposal proposal)
    {
        var refreshed = McpAttachmentContents.Refresh(Root, proposal.Workspace);
        if (ReferenceEquals(refreshed, proposal.Workspace))
        {
            return proposal;
        }

        return proposal switch
        {
            McpProposal strict => strict with { Transaction = strict.Transaction with { Workspace = refreshed } },
            McpAuthoringProposal authored => authored with { Result = authored.Result with { Workspace = refreshed, ExecutableReady = refreshed.Compilation.Success, ExecutableDiagnostics = [.. refreshed.Compilation.Diagnostics] } },
            _ => proposal
        };
    }

    ScreenplayWorkspace CheckedCurrent(JsonElement arguments)
    {
        var workspace = Current();
        if (McpJson.RequiredString(arguments, "expectedRevision") != workspace.Revision.ToString())
        {
            throw new McpFailure("StaleRevision: reopen the workspace before continuing.") { FailureKind = "StaleRevision" };
        }

        Root.Verify(workspace);
        return workspace;
    }

    ScreenplayWorkspace OpenFromDisk(string name)
    {
        var documents = Root.Read(allowEmpty: true);
        var identity = ApplicationIdentity.Create(name);
        if (documents.IsEmpty)
        {
            return ScreenplayWorkspace.CreateEmpty(identity, name);
        }

        var loaded = McpAttachmentContents.Load(Root, documents);
        return ScreenplayWorkspace.Create(identity, name, documents, SemanticIdentityCatalog.Empty(identity), loaded.Contents, loaded.Diagnostics);
    }

    ScreenplayWorkspace Current()
    {
        McpRecoveryJournal.RefusePending(Root);
        var workspace = _workspace ?? throw new McpFailure("Open a workspace first.");
        new McpManagedFiles(Root).Verify(McpState.FileName, _stateBytes);
        var refreshed = McpAttachmentContents.Refresh(Root, workspace);
        if (!ReferenceEquals(workspace, refreshed))
        {
            _workspace = refreshed;
        }

        return refreshed;
    }
}

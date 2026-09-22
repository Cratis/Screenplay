// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

sealed class McpDisk(McpRoot root, Action<string, string>? move = null)
{
    readonly Action<string, string> _move = move ?? ((source, destination) => File.Move(source, destination));

    internal McpDiskResult Apply(IMcpProposal proposal, McpStatePlan? state = null)
    {
        if (!proposal.Accepted || proposal.WritePlan.BeforeRevision != proposal.Before.Revision ||
            proposal.WritePlan.BeforeCatalogRevision != proposal.Before.IdentityCatalog.Revision ||
            proposal.WritePlan.AfterRevision != proposal.Workspace.Revision ||
            proposal.WritePlan.AfterCatalogRevision != proposal.Workspace.IdentityCatalog.Revision)
        {
            throw new McpFailure("InvalidProposal: the server proposal is not a successful revision-bound transaction.");
        }

        McpRecoveryJournal.RefusePending(root);
        var files = new McpManagedFiles(root);
        state ??= new(files.Read(McpState.FileName), McpState.Serialize(proposal.Workspace));
        files.Verify(McpState.FileName, state.Before);
        root.Verify(proposal.Before);
        CheckDestinations(proposal);
        var journal = McpRecoveryJournal.Prepare(root, proposal, state);
        var changes = journal.Changes;
        try
        {
            Stage(changes);
            McpManagedFiles.WritePrivate(journal.StateStage, state.After);
            journal.VerifyMarker();
            root.Verify(proposal.Before);
            files.Verify(McpState.FileName, state.Before);
            CheckDestinations(proposal);
            Backup(changes, journal);
            Install(changes, journal);
            InstallState(files, state, journal);
            root.Verify(proposal.Workspace);
            files.Verify(McpState.FileName, state.After);
            journal.Complete(applied: true);
        }
        catch (Exception exception)
        {
            var recovery = new List<string> { $"Apply failed: {exception.Message}" };
            var restored = false;
            try
            {
                new McpRecovery(root, journal).Rollback();
                restored = true;
            }
            catch (Exception failure)
            {
                recovery.Add($"RecoveryRequired: {failure.Message}. Preserve .screenplay/pending.json and all backup files; inspect workspace-state before explicit rollback.");
                recovery.AddRange(changes.Where(change => change.Backup is not null).Select(change => $"Recovery backup: '{change.Backup}'"));
                recovery.Add($"Identity backup: '{journal.StateBackup}'");
            }

            return new(false, restored ? "RolledBack" : "RecoveryRequired", recovery, changes.Length, changes.Count(change => change.Installed));
        }

        return new(true, $"Applied {changes.Length} document changes and durable identity state", [], changes.Length, changes.Count(change => change.Installed));
    }

    static void VerifyBytes(string path, WorkspaceDocument document)
    {
        if (!McpManagedFiles.Equal(McpManagedFiles.ReadPath(path, McpRoot.MaximumBytes), [.. document.Bytes]))
        {
            throw new McpFailure($"DiskDrift: '{document.Path}' changed.");
        }
    }

    void CheckDestinations(IMcpProposal proposal)
    {
        var before = proposal.Before.Documents.Select(document => document.Path.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var document in proposal.Workspace.Documents)
        {
            var path = root.PathFor(document.Path);
            McpManagedFiles.CheckExisting(path);
            if (!before.Contains(document.Path.Value) && (File.Exists(path) || Directory.Exists(path)))
            {
                throw new McpFailure($"DestinationOccupied: '{document.Path}' is not owned by this workspace.");
            }
        }
    }

    void Stage(IEnumerable<McpDiskChange> changes)
    {
        foreach (var change in changes.Where(change => change.Entry.After is not null))
        {
            _ = root.PathFor(change.Entry.After!.Path, createParents: true);
            McpManagedFiles.WritePrivate(change.Stage!, [.. change.Entry.After.Bytes]);
        }
    }

    void Backup(IEnumerable<McpDiskChange> changes, McpRecoveryJournal journal)
    {
        foreach (var change in changes.Where(change => change.Entry.Before is not null))
        {
            journal.VerifyMarker();
            var before = change.Entry.Before!;
            var path = root.PathFor(before.Path);
            VerifyBytes(path, before);
            McpManagedFiles.CheckExisting(change.Backup!);
            _move(path, change.Backup!);
        }
    }

    void Install(IEnumerable<McpDiskChange> changes, McpRecoveryJournal journal)
    {
        foreach (var change in changes.Where(change => change.Entry.After is not null))
        {
            journal.VerifyMarker();
            var path = root.PathFor(change.Entry.After!.Path);
            VerifyBytes(change.Stage!, change.Entry.After);
            if (change.Backup is not null)
            {
                VerifyBytes(change.Backup, change.Entry.Before!);
                McpFileAccess.Preserve(change.Backup, change.Stage!);
            }

            _move(change.Stage!, path);
            change.Installed = true;
        }
    }

    void InstallState(McpManagedFiles files, McpStatePlan state, McpRecoveryJournal journal)
    {
        journal.VerifyMarker();
        files.Verify(McpState.FileName, state.Before);
        if (state.Before is not null)
        {
            _move(files.PathFor(McpState.FileName), journal.StateBackup);
            McpFileAccess.Preserve(journal.StateBackup, journal.StateStage);
        }

        if (!McpManagedFiles.Equal(McpManagedFiles.ReadPath(journal.StateStage, McpManagedFiles.MaximumStateBytes), state.After))
        {
            throw new McpFailure("IdentityStateDrift: staged identities changed before installation.");
        }

        _move(journal.StateStage, files.PathFor(McpState.FileName));
    }
}

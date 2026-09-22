// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

sealed class McpDisk(McpRoot root, Action<string, string>? move = null)
{
    readonly Action<string, string> _move = move ?? ((source, destination) => File.Move(source, destination));

    internal McpDiskResult Apply(McpProposal proposal)
    {
        var transaction = proposal.Transaction;
        if (!transaction.Success || !transaction.Workspace!.Compilation.Success || transaction.WritePlan!.BeforeRevision != proposal.Before.Revision ||
            transaction.WritePlan.BeforeCatalogRevision != proposal.Before.IdentityCatalog.Revision)
        {
            throw new McpFailure("InvalidProposal: the server proposal is not a successful revision-bound transaction.");
        }

        root.Verify(proposal.Before);
        var changes = transaction.WritePlan.Entries.Select(entry => new McpDiskChange(entry)).ToArray();
        CheckDestinations(proposal);
        var recovery = new List<string>();
        try
        {
            Stage(changes);
            root.Verify(proposal.Before);
            CheckDestinations(proposal);
            Backup(changes);
            Install(changes);
            root.Verify(transaction.Workspace);
        }
        catch (Exception exception)
        {
            recovery.Add($"Apply failed: {exception.Message}");
            var restored = Rollback(changes, recovery);
            CleanupStages(changes, recovery);
            try
            {
                root.Verify(proposal.Before);
            }
            catch (Exception verification)
            {
                restored = false;
                recovery.Add($"Original workspace could not be verified after rollback: {verification.Message}");
            }

            return new(false, restored ? "RolledBack" : "RecoveryRequired", recovery, changes.Length, changes.Count(change => change.Installed));
        }

        // The applied snapshot is verified before any backup is discarded. Cleanup failures are explicit.
        foreach (var change in changes.Where(change => change.Backup is not null))
        {
            try
            {
                File.Delete(change.Backup!);
            }
            catch (Exception exception)
            {
                recovery.Add($"Applied; retained backup '{change.Backup}': {exception.Message}");
            }
        }

        return new(true, recovery.Count == 0 ? $"Applied {changes.Length} document changes" : "AppliedWithRetainedBackups", recovery, changes.Length, changes.Count(change => change.Installed));
    }

    static void CleanupStages(IEnumerable<McpDiskChange> changes, List<string> recovery)
    {
        foreach (var change in changes.Where(change => change.Stage is not null))
        {
            try
            {
                File.Delete(change.Stage!);
            }
            catch (Exception exception)
            {
                recovery.Add($"Retained staging file '{change.Stage}': {exception.Message}");
            }
        }
    }

    static void VerifyBytes(string path, WorkspaceDocument document)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length != document.Bytes.Length)
        {
            throw new McpFailure($"DiskDrift: '{document.Path}' changed.");
        }

        var bytes = new byte[document.Bytes.Length];
        stream.ReadExactly(bytes);
        if (!bytes.AsSpan().SequenceEqual(document.Bytes.AsSpan()) || stream.ReadByte() != -1)
        {
            throw new McpFailure($"DiskDrift: '{document.Path}' changed.");
        }
    }

    void CheckDestinations(McpProposal proposal)
    {
        var before = proposal.Before.Documents.Select(document => document.Path.Value).ToHashSet(StringComparer.Ordinal);
        foreach (var document in proposal.Transaction.Workspace!.Documents)
        {
            var path = root.PathFor(document.Path);
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
            var after = change.Entry.After!;
            var destination = root.PathFor(after.Path, true);
            var stage = $"{destination}.screenplay-mcp-{Guid.NewGuid():N}.stage";
            using var stream = new FileStream(stage, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            change.Stage = stage;
            stream.Write(after.Bytes.AsSpan());
            stream.Flush(true);
        }
    }

    void Backup(IEnumerable<McpDiskChange> changes)
    {
        foreach (var change in changes.Where(change => change.Entry.Before is not null))
        {
            var before = change.Entry.Before!;
            var path = root.PathFor(before.Path);
            VerifyBytes(path, before);
            var backup = $"{path}.screenplay-mcp-{Guid.NewGuid():N}.backup";
            _move(path, backup);
            change.Backup = backup;
        }
    }

    void Install(IEnumerable<McpDiskChange> changes)
    {
        foreach (var change in changes.Where(change => change.Entry.After is not null))
        {
            var path = root.PathFor(change.Entry.After!.Path);
            _move(change.Stage!, path);
            change.Installed = true;
            change.Stage = null;
        }
    }

    bool Rollback(IEnumerable<McpDiskChange> changes, List<string> recovery)
    {
        var restored = true;
        foreach (var change in changes.Reverse().Where(change => change.Installed))
        {
            try
            {
                var path = root.PathFor(change.Entry.After!.Path);
                VerifyBytes(path, change.Entry.After);
                File.Delete(path);
            }
            catch (Exception exception)
            {
                restored = false;
                recovery.Add($"Cannot remove installed '{change.Entry.After!.Path}': {exception.Message}");
            }
        }

        foreach (var change in changes.Where(change => change.Backup is not null))
        {
            try
            {
                var path = root.PathFor(change.Entry.Before!.Path);

                // No overwrite: a concurrently created file must not be destroyed by recovery.
                VerifyBytes(change.Backup!, change.Entry.Before);
                File.Move(change.Backup!, path);
                change.Backup = null;
            }
            catch (Exception exception)
            {
                restored = false;
                recovery.Add($"Restore '{change.Entry.Before!.Path}' from '{change.Backup}': {exception.Message}");
            }
        }

        return restored;
    }
}

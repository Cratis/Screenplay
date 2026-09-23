// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpRecoveryJournal
{
    internal const string FileName = "pending.json";
    readonly McpRoot _root;
    readonly McpManagedFiles _files;
    readonly byte[] _bytes;

    McpRecoveryJournal(McpRoot root, McpRecoveryRecord record, byte[] bytes)
    {
        _root = root;
        _files = new(root);
        _bytes = bytes;
        Record = record;
        if (record.Version != 1 || !Guid.TryParseExact(record.OperationId, "N", out _))
        {
            throw new McpFailure("Unsupported or invalid recovery journal.");
        }

        Before = McpWorkspaceTransport.Restore(record.BeforeWorkspace);
        After = McpWorkspaceTransport.Restore(record.AfterWorkspace);
        foreach (var document in Before.Documents.Concat(After.Documents))
        {
            _ = root.PathFor(document.Path);
        }

        if ((record.BeforeState is not null && !McpManagedFiles.Equal(record.BeforeState, McpState.Serialize(Before))) ||
            !McpManagedFiles.Equal(record.AfterState, McpState.Serialize(After)))
        {
            throw new McpFailure("Recovery journal identity state does not match its canonical workspace snapshots.");
        }

        if (record.Access is null || record.Access.Length != Before.Documents.Length ||
            record.Access.Select(access => access.Path).Distinct(StringComparer.Ordinal).Count() != record.Access.Length ||
            record.Access.Any(access => !Before.Documents.Any(document => document.Path.Value == access.Path)))
        {
            throw new McpFailure("Recovery access rules do not match the original documents.");
        }

        foreach (var access in record.Access)
        {
            access.Validate();
        }

        if ((record.BeforeState is null) != (record.StateAccess is null) ||
            (record.StateAccess is not null && record.StateAccess.Path != ".screenplay/identities.json"))
        {
            throw new McpFailure("Recovery identity-state access rules do not match the original metadata.");
        }

        record.StateAccess?.Validate();
        Changes = [.. Differences(Before, After).Select(entry => new McpDiskChange(entry)
        {
            Stage = entry.After is null ? null : Artifact(entry.After, "stage"),
            Backup = entry.Before is null ? null : Artifact(entry.Before, "backup")
        })];
    }

    internal McpRecoveryRecord Record { get; }
    internal ScreenplayWorkspace Before { get; }
    internal ScreenplayWorkspace After { get; }
    internal McpDiskChange[] Changes { get; }
    internal string StateStage => _files.PathFor($"{Record.OperationId}.stage");
    internal string StateBackup => _files.PathFor($"{Record.OperationId}.backup");

    internal static void RefusePending(McpRoot root)
    {
        if (new McpManagedFiles(root).Read(FileName, McpManagedFiles.MaximumJournalBytes) is not null)
        {
            throw new McpFailure("PendingOperation: .screenplay/pending.json records an interrupted or uncertain apply. Use workspace-state, then explicitly recover-workspace with its operationId before opening or editing.");
        }
    }

    internal static McpRecoveryJournal? Load(McpRoot root)
    {
        var bytes = new McpManagedFiles(root).Read(FileName, McpManagedFiles.MaximumJournalBytes);
        if (bytes is null)
        {
            return null;
        }

        try
        {
            var record = JsonSerializer.Deserialize<McpRecoveryRecord>(bytes) ?? throw new McpFailure("Empty recovery record.");
            if (!bytes.AsSpan().SequenceEqual(JsonSerializer.SerializeToUtf8Bytes(record)))
            {
                throw new McpFailure("Recovery record is not canonical.");
            }

            return new(root, record, bytes);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            throw new McpFailure($"RecoveryJournalConflict: preserve .screenplay/pending.json and all .backup files for manual recovery. {exception.Message}");
        }
    }

    internal static McpRecoveryJournal Prepare(McpRoot root, IMcpProposal proposal, McpStatePlan state)
    {
        RefusePending(root);
        var files = new McpManagedFiles(root);
        var record = new McpRecoveryRecord(
            1,
            Guid.NewGuid().ToString("N"),
            Encoding.UTF8.GetString(McpWorkspaceTransport.ExportBytes(proposal.Before)),
            Encoding.UTF8.GetString(McpWorkspaceTransport.ExportBytes(proposal.Workspace)),
            state.Before,
            state.After,
            [.. proposal.Before.Documents.Select(document => McpRecoveryAccess.Capture(document.Path.Value, root.PathFor(document.Path)))],
            state.Before is null ? null : McpRecoveryAccess.Capture(".screenplay/identities.json", files.PathFor(McpState.FileName)));
        var bytes = JsonSerializer.SerializeToUtf8Bytes(record);
        if (bytes.Length > McpManagedFiles.MaximumJournalBytes)
        {
            throw new McpFailure("Recovery journal exceeds the bounded 64 MiB operation envelope; choose a smaller workspace.");
        }

        var journal = new McpRecoveryJournal(root, record, bytes);
        files.Verify(McpState.FileName, state.Before);
        root.Verify(proposal.Before);
        var temporary = files.PathFor($"{record.OperationId}.journal", create: true);
        McpManagedFiles.WritePrivate(temporary, bytes);
        File.Move(temporary, files.PathFor(FileName));
        journal.VerifyMarker();
        return journal;
    }

    internal static void DeleteKnown(string? path, byte[]? expected)
    {
        if (path is null)
        {
            return;
        }

        var actual = McpManagedFiles.ReadPath(path, McpManagedFiles.MaximumStateBytes);
        if (actual is not null)
        {
            if (!McpManagedFiles.Equal(actual, expected))
            {
                throw new McpFailure($"RecoveryConflict: unexpected bytes in '{path}'; retained without modification.");
            }

            File.Delete(path);
        }
    }

    internal void VerifyMarker()
    {
        if (!McpManagedFiles.Equal(_files.Read(FileName, McpManagedFiles.MaximumJournalBytes), _bytes))
        {
            throw new McpFailure("RecoveryJournalDrift: the pending-operation marker changed; preserve all recovery files.");
        }
    }

    internal void VerifyOriginalAccess()
    {
        foreach (var access in Record.Access)
        {
            access.Verify(_root.PathFor(PortablePlayPath.Parse(access.Path)));
        }

        Record.StateAccess?.Verify(_files.PathFor(McpState.FileName));
    }

    internal void Complete(bool applied)
    {
        VerifyMarker();
        _root.Verify(applied ? After : Before);
        _files.Verify(McpState.FileName, applied ? Record.AfterState : Record.BeforeState);
        if (!applied)
        {
            VerifyOriginalAccess();
        }

        foreach (var change in Changes)
        {
            DeleteKnown(change.Stage, change.Entry.After?.Bytes.ToArray());
            DeleteKnown(change.Backup, change.Entry.Before?.Bytes.ToArray());
        }

        DeleteKnown(StateStage, Record.AfterState);
        DeleteKnown(StateBackup, Record.BeforeState);
        _root.Verify(applied ? After : Before);
        _files.Verify(McpState.FileName, applied ? Record.AfterState : Record.BeforeState);
        if (!applied)
        {
            VerifyOriginalAccess();
        }

        VerifyMarker();
        File.Delete(_files.PathFor(FileName));
    }
}

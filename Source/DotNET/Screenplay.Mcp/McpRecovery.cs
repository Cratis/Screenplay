// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed class McpRecovery(McpRoot root, McpRecoveryJournal journal)
{
    readonly McpManagedFiles _files = new(root);

    internal object Status()
    {
        try
        {
            Preflight();
            return new { pending = true, operationId = journal.Record.OperationId, canRollback = true, conflict = (string?)null };
        }
        catch (Exception exception)
        {
            return new { pending = true, operationId = journal.Record.OperationId, canRollback = false, conflict = exception.Message };
        }
    }

    internal void Rollback()
    {
        // No writes (including cleanup) until every path and artifact has passed the complete preflight.
        Preflight();
        foreach (var path in Paths())
        {
            journal.VerifyMarker();
            var before = Document(journal.Before, path);
            var after = Document(journal.After, path);
            var destination = root.PathFor(PortablePlayPath.Parse(path));
            var actual = ReadKnown(destination, before, after);
            if (actual is not null && !McpManagedFiles.Equal(actual, before))
            {
                File.Delete(destination);
            }
        }

        for (var index = 0; index < journal.Before.Documents.Length; index++)
        {
            journal.VerifyMarker();
            var document = journal.Before.Documents[index];
            var destination = root.PathFor(document.Path, createParents: true);
            var before = document.Bytes.ToArray();
            var current = ReadKnown(destination, before, Document(journal.After, document.Path.Value));
            if (McpManagedFiles.Equal(current, before))
            {
                journal.Record.Access.Single(access => access.Path == document.Path.Value).Restore(destination);
                continue;
            }

            if (current is not null)
            {
                throw new McpFailure($"RecoveryConflict: '{document.Path}' changed during rollback.");
            }

            var temporary = RestoreStage(index);
            StageOriginal(temporary, before);
            journal.Record.Access.Single(access => access.Path == document.Path.Value).Restore(temporary);
            File.Move(temporary, root.PathFor(document.Path));
        }

        RestoreState();
        root.Verify(journal.Before);
        _files.Verify(McpState.FileName, journal.Record.BeforeState);
        journal.VerifyOriginalAccess();
        CleanupRestoreStages();
        journal.Complete(applied: false);
    }

    static byte[]? Document(ScreenplayWorkspace workspace, string path) => workspace.Documents.SingleOrDefault(document => document.Path.Value == path)?.Bytes.ToArray();

    static byte[]? ReadKnown(string path, byte[]? before, byte[]? after)
    {
        var actual = McpManagedFiles.ReadPath(path, McpManagedFiles.MaximumStateBytes);
        if (actual is not null && !McpManagedFiles.Equal(actual, before) && !McpManagedFiles.Equal(actual, after))
        {
            throw new McpFailure($"RecoveryConflict: '{path}' contains unexpected external bytes; nothing may overwrite them.");
        }

        return actual;
    }

    static void CheckArtifact(string? path, byte[]? expected)
    {
        if (path is not null)
        {
            _ = ReadKnown(path, expected, expected);
        }
    }

    static void StageOriginal(string path, byte[] bytes)
    {
        var existing = ReadKnown(path, bytes, bytes);
        if (existing is null)
        {
            McpManagedFiles.WritePrivate(path, bytes);
        }
    }

    void Preflight()
    {
        journal.VerifyMarker();
        var paths = Paths().ToHashSet(StringComparer.Ordinal);
        var actual = root.Read(allowEmpty: true);
        if (actual.Any(document => !paths.Contains(document.Path.Value)))
        {
            throw new McpFailure("RecoveryConflict: an unexpected .play file exists; no files were restored or removed.");
        }

        var affected = journal.Changes.SelectMany(change => new[] { change.Entry.Before?.Path.Value, change.Entry.After?.Path.Value }).OfType<string>().ToHashSet(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var content = ReadKnown(root.PathFor(PortablePlayPath.Parse(path)), Document(journal.Before, path), Document(journal.After, path));
            if (content is null && !affected.Contains(path))
            {
                throw new McpFailure($"RecoveryConflict: unchanged source '{path}' disappeared outside this transaction.");
            }
        }

        _ = ReadKnown(_files.PathFor(McpState.FileName), journal.Record.BeforeState, journal.Record.AfterState);
        foreach (var change in journal.Changes)
        {
            CheckArtifact(change.Stage, change.Entry.After?.Bytes.ToArray());
            CheckArtifact(change.Backup, change.Entry.Before?.Bytes.ToArray());
        }

        CheckArtifact(journal.StateStage, journal.Record.AfterState);
        CheckArtifact(journal.StateBackup, journal.Record.BeforeState);
        for (var index = 0; index < journal.Before.Documents.Length; index++)
        {
            CheckArtifact(RestoreStage(index), [.. journal.Before.Documents[index].Bytes]);
        }

        CheckArtifact(StateRestoreStage(), journal.Record.BeforeState);
        journal.VerifyMarker();
    }

    void RestoreState()
    {
        journal.VerifyMarker();
        var path = _files.PathFor(McpState.FileName);
        var actual = ReadKnown(path, journal.Record.BeforeState, journal.Record.AfterState);
        if (McpManagedFiles.Equal(actual, journal.Record.BeforeState))
        {
            journal.Record.StateAccess?.Restore(path);
            return;
        }

        if (actual is not null)
        {
            File.Delete(path);
        }

        if (journal.Record.BeforeState is not null)
        {
            StageOriginal(StateRestoreStage(), journal.Record.BeforeState);
            journal.Record.StateAccess!.Restore(StateRestoreStage());
            File.Move(StateRestoreStage(), _files.PathFor(McpState.FileName));
        }
    }

    void CleanupRestoreStages()
    {
        for (var index = 0; index < journal.Before.Documents.Length; index++)
        {
            McpRecoveryJournal.DeleteKnown(RestoreStage(index), [.. journal.Before.Documents[index].Bytes]);
        }

        McpRecoveryJournal.DeleteKnown(StateRestoreStage(), journal.Record.BeforeState);
    }

    string RestoreStage(int index) => _files.PathFor($"{journal.Record.OperationId}-{index}.rollback");
    string StateRestoreStage() => _files.PathFor($"{journal.Record.OperationId}.state-rollback");
    IEnumerable<string> Paths() => journal.Before.Documents.Concat(journal.After.Documents).Select(document => document.Path.Value).Distinct(StringComparer.Ordinal);
}

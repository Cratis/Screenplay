// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpDisk
{
    static void StageNotes(McpRecoveryJournal journal)
    {
        if (journal.Record.Notes is { } notes) McpManagedFiles.WritePrivate(journal.NotesStage, notes.After);
    }

    void InstallNotes(McpManagedFiles files, McpRecoveryJournal journal)
    {
        if (journal.Record.Notes is not { } notes) return;
        journal.VerifyMarker();
        journal.VerifyNotes(applied: false);
        if (notes.Before is not null)
        {
            _move(files.PathFor(ModelNotesStore.FileName), journal.NotesBackup);
            McpFileAccess.Preserve(journal.NotesBackup, journal.NotesStage);
        }

        if (!McpManagedFiles.Equal(McpManagedFiles.ReadPath(journal.NotesStage, ModelNotesStore.MaximumBytes), notes.After))
        {
            throw new McpFailure("Staged notes changed before installation.") { FailureKind = "NotesRevisionDrift" };
        }

        _move(journal.NotesStage, files.PathFor(ModelNotesStore.FileName));
        journal.VerifyNotes(applied: true);
    }
}

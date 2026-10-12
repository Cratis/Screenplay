// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpRecovery
{
    void PreflightNotes()
    {
        if (journal.Record.Notes is not { } notes) return;
        _ = ReadKnown(_files.PathFor(ModelNotesStore.FileName), notes.Before, notes.After);
        CheckArtifact(journal.NotesStage, notes.After);
        CheckArtifact(journal.NotesBackup, notes.Before);
        CheckArtifact(journal.NotesRestoreStage, notes.Before);
    }

    void RestoreNotes()
    {
        if (journal.Record.Notes is not { } notes) return;
        journal.VerifyMarker();
        var path = _files.PathFor(ModelNotesStore.FileName);
        var actual = ReadKnown(path, notes.Before, notes.After);
        if (McpManagedFiles.Equal(actual, notes.Before))
        {
            notes.Access?.Restore(path);
            return;
        }

        if (actual is not null) File.Delete(path);
        if (notes.Before is not null)
        {
            StageOriginal(journal.NotesRestoreStage, notes.Before);
            notes.Access!.Restore(journal.NotesRestoreStage);
            File.Move(journal.NotesRestoreStage, _files.PathFor(ModelNotesStore.FileName));
        }

        journal.VerifyNotes(applied: false);
    }
}

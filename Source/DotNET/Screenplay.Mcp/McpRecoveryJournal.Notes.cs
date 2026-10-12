// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpRecoveryJournal
{
    internal string NotesStage => OperationPath(_root, Record.OperationId, "notes-stage");
    internal string NotesBackup => OperationPath(_root, Record.OperationId, "notes-backup");
    internal string NotesRestoreStage => OperationPath(_root, Record.OperationId, "notes-rollback");

    internal void VerifyNotes(bool applied)
    {
        if (Record.Notes is { } notes) _files.Verify(ModelNotesStore.FileName, applied ? notes.After : notes.Before);
    }

    void ValidateNotes()
    {
        if (Record.Notes is not { } notes) return;
        var before = ModelNotesStore.ReadBytes(notes.Before, Before.IdentityCatalog.Application);
        var after = ModelNotesStore.ReadBytes(notes.After, After.IdentityCatalog.Application);
        if (before.Status == "unreadable" || after.Status != "ok" ||
            !notes.After.AsSpan().SequenceEqual(ModelNotesStore.Serialize(after.Document!)) ||
            (notes.Before is null) != (notes.Access is null) ||
            (notes.Access is not null && notes.Access.Path != ".screenplay/notes.json"))
        {
            throw new McpFailure("Recovery journal notes or access rules are invalid.");
        }

        notes.Access?.Validate();
    }

    void CleanupNotes()
    {
        if (Record.Notes is not { } notes) return;
        DeleteKnown(NotesStage, notes.After);
        DeleteKnown(NotesBackup, notes.Before);
        DeleteKnown(NotesRestoreStage, notes.Before);
    }
}

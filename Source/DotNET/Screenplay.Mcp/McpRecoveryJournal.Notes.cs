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

    static int JournalVersion(IMcpProposal proposal) => proposal switch
    {
        McpNotesProposal => 2,
        McpReleaseBaselineProposal => 3,
        _ => 1
    };

    static McpNotesJournal? NotesRecord(IMcpProposal proposal, McpManagedFiles files)
    {
        if (proposal is not McpNotesProposal notes) return null;
        var access = notes.Notes.Before is null ? null : McpRecoveryAccess.Capture(".screenplay/notes.json", files.PathFor(ModelNotesStore.FileName));
        return new(notes.Notes.Before, notes.Notes.After, access);
    }

    static void RequireJournalVersion(McpRecoveryRecord record)
    {
        switch (record.Version)
        {
            case 1:
                if (record.Notes is not null || record.ReleaseBaseline is not null) throw new McpFailure("Version 1 recovery journals cannot carry sidecars.");
                break;
            case 2:
                if (record.Notes is null || record.ReleaseBaseline is not null) throw new McpFailure("Version 2 recovery journals require only notes.");
                break;
            case 3:
                if (record.ReleaseBaseline is null || record.Notes is not null) throw new McpFailure("Version 3 recovery journals require only a release baseline.");
                break;
            default: throw new McpFailure("Unsupported or invalid recovery journal.");
        }
    }

    static void RequireReadableNotes(ModelNotesRead before, ModelNotesRead after)
    {
        if (before.Status == "unreadable") throw InvalidNotes();
        if (after.Status != "ok") throw InvalidNotes();
    }

    static void RequireNotesImage(McpNotesJournal notes, ModelNotesRead after)
    {
        if (!notes.After.AsSpan().SequenceEqual(ModelNotesStore.Serialize(after.Document!))) throw InvalidNotes();
    }

    static void RequireNotesAccess(McpNotesJournal notes)
    {
        if ((notes.Before is null) != (notes.Access is null)) throw InvalidNotes();
        if (!KnownNotesAccess(notes.Access)) throw InvalidNotes();
    }

    static bool KnownNotesAccess(McpRecoveryAccess? access)
    {
        if (access is null) return true;
        return access.Path == ".screenplay/notes.json";
    }

    static McpFailure InvalidNotes() => new("Recovery journal notes or access rules are invalid.");

    void ValidateNotes()
    {
        if (Record.Notes is not { } notes) return;
        var before = ModelNotesStore.ReadBytes(notes.Before, Before.IdentityCatalog.Application);
        var after = ModelNotesStore.ReadBytes(notes.After, After.IdentityCatalog.Application);
        RequireReadableNotes(before, after);
        RequireNotesImage(notes, after);
        RequireNotesAccess(notes);
        notes.Access?.Validate();
    }

    void CleanupNotes()
    {
        if (Record.Notes is not { } notes) return;
        DeleteKnown(NotesStage, notes.After);
        DeleteKnown(NotesBackup, notes.Before);
        DeleteKnown(NotesRestoreStage, notes.Before);
    }

    void VerifyNotesAccess() => Record.Notes?.Access?.Verify(_files.PathFor(ModelNotesStore.FileName));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_recovering_an_interrupted_notes_write : given.a_notes_workspace
{
    McpRecoveryJournal _journal = null!;

    void Establish()
    {
        var after = ModelNotesStore.Serialize(Notes with { Notes = [Notes.Notes[0] with { Text = "Changed question" }] });
        var proposal = new McpNotesProposal(Original, new(NotesBytes, after, ModelNotesStore.Revision(NotesBytes), ModelNotesStore.Revision(after)));
        _journal = McpRecoveryJournal.Prepare(Root, proposal, new(OriginalState, OriginalState));
        McpManagedFiles.WritePrivate(_journal.NotesStage, after);
        File.Move(Files.PathFor(ModelNotesStore.FileName), _journal.NotesBackup);
        File.Move(_journal.NotesStage, Files.PathFor(ModelNotesStore.FileName));
    }

    void Because() => new McpRecovery(Root, McpRecoveryJournal.Load(Root)!).Rollback();

    [Fact] void should_restore_exact_notes() => McpManagedFiles.Equal(Files.Read(ModelNotesStore.FileName), NotesBytes).ShouldBeTrue();
    [Fact] void should_restore_identity_state() => McpManagedFiles.Equal(Files.Read(McpState.FileName), OriginalState).ShouldBeTrue();
    [Fact] void should_preserve_source() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_clear_the_marker() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
    [Fact] void should_remove_notes_backup() => File.Exists(_journal.NotesBackup).ShouldBeFalse();
}

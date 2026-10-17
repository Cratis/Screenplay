// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_recovering_notes_with_new_container_identities : given.an_unbound_persisted_notes_workspace
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_roll_back_both_state_and_notes_after_an_interrupted_or_unknown_outcome(bool unknownOutcome)
    {
        var notes = new ModelNotesDocument(Split.IdentityCatalog.Application.ToString(), [new(ModelNotesStore.NewId(), "question", "Ready?", new(Split.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice).Id.ToString()), null, "team", DateTimeOffset.UtcNow, [], null)]);
        var bytes = ModelNotesStore.Serialize(notes);
        var proposal = new McpNotesProposal(Split, new(null, bytes, ModelNotesStore.AbsentRevision, ModelNotesStore.Revision(bytes)));
        var journal = McpRecoveryJournal.Prepare(Root, proposal, new(EarlierState, McpState.Serialize(Split)));
        McpManagedFiles.WritePrivate(journal.NotesStage, bytes);
        File.Move(journal.NotesStage, Files.PathFor(ModelNotesStore.FileName));
        File.Move(Files.PathFor(McpState.FileName), journal.StateBackup);
        McpManagedFiles.WritePrivate(Files.PathFor(McpState.FileName), journal.Record.AfterState);
        var recovered = new McpRecovery(Root, McpRecoveryJournal.Load(Root)!);
        if (unknownOutcome)
        {
            // Simulate a lost response: inspect pending state before choosing rollback, never retry apply.
            JsonSerializer.SerializeToElement(recovered.Status()).GetProperty("canRollback").GetBoolean().ShouldBeTrue();
            Catch.Exception(() => new McpWorkspaces(Root).Open(McpJson.Empty)).ShouldNotBeNull();
        }

        recovered.Rollback();
        McpManagedFiles.Equal(Files.Read(McpState.FileName), EarlierState).ShouldBeTrue();
        Files.Read(ModelNotesStore.FileName).ShouldBeNull();
        Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
        File.Exists(journal.StateBackup).ShouldBeFalse();
        Root.Verify(Split);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_notes_change_after_proposing : given.a_notes_workspace
{
    JsonElement _proposal;
    Exception? _failure;
    byte[] _external = [];

    void Establish()
    {
        _proposal = ProposeNotes([new { operation = "edit", id = Notes.Notes[0].Id, text = "Changed" }]);
        _external = ModelNotesStore.Serialize(Notes with { Notes = [Notes.Notes[0] with { Text = "External" }] });
        File.WriteAllBytes(Files.PathFor(ModelNotesStore.FileName), _external);
    }

    void Because() => _failure = Catch.Exception(() => Apply(_proposal));

    [Fact] void should_refuse_stale_notes() => ((McpFailure)_failure!).FailureKind.ShouldEqual("NotesRevisionDrift");
    [Fact] void should_not_create_a_journal() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
    [Fact] void should_keep_external_notes() => McpManagedFiles.Equal(Files.Read(ModelNotesStore.FileName), _external).ShouldBeTrue();
}

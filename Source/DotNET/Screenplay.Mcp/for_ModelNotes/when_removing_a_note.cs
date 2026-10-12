// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_removing_a_note : given.a_notes_workspace
{
    ModelNotesRead _notes = null!;

    void Because()
    {
        _ = Apply(ProposeNotes([new { operation = "remove", id = Notes.Notes[0].Id }]));
        _notes = ModelNotesStore.ReadBytes(Files.Read(ModelNotesStore.FileName), Original.IdentityCatalog.Application);
    }

    [Fact] void should_keep_a_valid_empty_document() => _notes.Status.ShouldEqual("ok");
    [Fact] void should_remove_only_the_selected_note() => _notes.Document!.Notes.ShouldBeEmpty();
    [Fact] void should_distinguish_empty_from_absent() => (_notes.Revision != ModelNotesStore.AbsentRevision).ShouldBeTrue();
}

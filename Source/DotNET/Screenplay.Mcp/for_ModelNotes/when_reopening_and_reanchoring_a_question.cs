// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_reopening_and_reanchoring_a_question : given.a_notes_workspace
{
    ModelNote _note = null!;

    void Because()
    {
        var proposal = ProposeNotes([
            new { operation = "resolve", id = Notes.Notes[0].Id, resolution = new { by = "dismissed", text = "Not needed", author = "team" } },
            new { operation = "reopen", id = Notes.Notes[0].Id },
            new { operation = "edit", id = Notes.Notes[0].Id, text = "Which application policy applies?" },
            new { operation = "reanchor", id = Notes.Notes[0].Id, anchor = new { application = true } }
        ]);
        _ = Apply(proposal);
        _note = ModelNotesStore.ReadBytes(Files.Read(ModelNotesStore.FileName), Original.IdentityCatalog.Application).Document!.Notes.Single();
    }

    [Fact] void should_clear_resolution() => _note.Resolution.ShouldBeNull();
    [Fact] void should_reanchor_explicitly() => _note.Anchor.Application.ShouldEqual(true);
    [Fact] void should_refresh_the_display_hint() => _note.LastAddress!.Kind.ShouldEqual("Application");
    [Fact] void should_preserve_the_note_identity() => _note.Id.ShouldEqual(Notes.Notes[0].Id);
    [Fact] void should_edit_the_text() => _note.Text.ShouldEqual("Which application policy applies?");
}

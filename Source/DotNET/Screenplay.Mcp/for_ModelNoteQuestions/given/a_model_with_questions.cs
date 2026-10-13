// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNoteQuestions.given;

public class a_model_with_questions : for_ReleaseBaselineCommands.given.a_release_workspace
{
    internal ModelNotesDocument Notes = null!;
    internal string NotesPath = null!;
    internal const string Scope = "Projects.Registration.RegisterProject";

    void Establish()
    {
        var slice = Original.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice);
        var unknown = SemanticId.Create(SemanticKind.Slice, "unknown");
        Notes = new(Original.IdentityCatalog.Application.ToString(),
        [
            Question(new(slice.Id.ToString()), "May a member cancel?"),
            Question(new(Application: true), "Who owns the application?"),
            Question(new(unknown.ToString()), "Orphaned question")
        ]);
        NotesPath = Files.PathFor(ModelNotesStore.FileName);
        WriteNotes();
    }

    internal void WriteNotes() => File.WriteAllBytes(NotesPath, ModelNotesStore.Serialize(Notes));

    static ModelNote Question(ModelNotesAnchor anchor, string text) => new(ModelNotesStore.NewId(), "question", text, anchor, null, "team", DateTimeOffset.UtcNow, [], null);
}

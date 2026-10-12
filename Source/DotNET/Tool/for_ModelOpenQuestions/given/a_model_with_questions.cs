// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.for_ModelOpenQuestions.given;

public class a_model_with_questions : for_ModelCheck.given.a_model
{
    protected string NotesPath = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(Root, "application.play"), "module M\n  feature F\n    slice StateChange Clean\n      event Recorded\n        value String\n    slice StateChange Other\n      event OtherRecorded\n        value String\n");
        var root = new McpRoot(Root);
        var workspace = ScreenplayWorkspace.Create("Model", root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model")));
        var files = new McpManagedFiles(root);
        McpManagedFiles.WritePrivate(files.PathFor(McpState.FileName, create: true), McpState.Serialize(workspace));
        var slice = workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice && assignment.Address.Name == "Clean");
        var other = workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice && assignment.Address.Name == "Other");
        var notes = new ModelNotesDocument(workspace.IdentityCatalog.Application.ToString(),
        [
            Question(new(slice.Id.ToString()), "May a member cancel?"),
            Question(new(Application: true), "Who owns the application?"),
            Question(new(other.Id.ToString()), "Unrelated question")
        ]);
        NotesPath = files.PathFor(ModelNotesStore.FileName);
        McpManagedFiles.WritePrivate(NotesPath, ModelNotesStore.Serialize(notes));
    }

    static ModelNote Question(ModelNotesAnchor anchor, string text) => new(ModelNotesStore.NewId(), "question", text, anchor, null, "team", DateTimeOffset.UtcNow, [], null);
}

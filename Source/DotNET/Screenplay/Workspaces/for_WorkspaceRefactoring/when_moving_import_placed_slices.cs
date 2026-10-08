// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_moving_import_placed_slices : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _literal = null!;
    WorkspaceAuthoringResult _glob = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("root", "application.play", "module Projects\n  feature Source\n    import \"steps/Register.play\"\n  feature Destination\n    slice StateChange Existing\n      command Existing"),
         Document("slice", "steps/Register.play", "slice StateChange Register\n  command Register")],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because()
    {
        var request = new WorkspaceMoveRequest
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = SemanticAddress.ForSlice(Workspace.IdentityCatalog.Application, "Projects", "Source", "Register"),
            NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Destination"),
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
        };
        _literal = Workspace.ProposeMove(request);
        var globWorkspace = ScreenplayWorkspace.Create("Projects",
            [.. Workspace.Documents.Select(document => document.Path.Value == "application.play"
            ? Document(document.StableKey, document.Path.Value, document.Text.Replace("steps/Register.play", "steps/*.play", StringComparison.Ordinal)) : document)],
            SemanticIdentityCatalog.Empty(Workspace.IdentityCatalog.Application));
        _glob = globWorkspace.ProposeMove(request with { ExpectedRevision = globWorkspace.Revision, ExpectedCatalogRevision = globWorkspace.IdentityCatalog.Revision });
    }

    [Fact] void should_accept_a_literal_import() => Assert.True(_literal.Accepted, string.Join(" | ", _literal.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_move_the_placing_import_to_the_new_parent() => _literal.Workspace.Documents.Single(document => document.Path.Value == "application.play").Text.Contains("feature Destination\n    import \"steps/Register.play\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_imported_file_in_place() => _literal.Workspace.Documents.Single(document => document.Path.Value == "steps/Register.play").Text.ShouldEqual("slice StateChange Register\n\n  command Register\n");
    [Fact] void should_keep_every_assigned_identity() => _literal.Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id).ShouldContainOnly(Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id));
    [Fact] void should_refuse_a_glob_placement() => _glob.Conflicts.Single().Message.Contains("glob", StringComparison.Ordinal).ShouldBeTrue();
}

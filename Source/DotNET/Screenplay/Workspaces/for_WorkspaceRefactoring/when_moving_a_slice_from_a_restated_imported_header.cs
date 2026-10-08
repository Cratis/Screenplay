// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_moving_a_slice_from_a_restated_imported_header : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("root", "application.play", "module Projects\n  import \"steps.play\"\n  feature Destination"),
         Document("steps", "steps.play", "feature Source\n  slice StateChange Moving\n    command Save")],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForSlice(Workspace.IdentityCatalog.Application, "Projects", "Source", "Moving"),
        NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Destination"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_accept() => Assert.True(_result.Accepted, string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_print_an_authored_destination_header_below_module_placement() => _result.Workspace.Documents.Single(document => document.Path.Value == "steps.play").Text.ShouldContain("feature Destination\n\n  slice StateChange Moving");
    [Fact] void should_keep_the_module_placing_import() => _result.Workspace.Documents.Single(document => document.Path.Value == "application.play").Text.ShouldEqual(Workspace.Documents.Single(document => document.Path.Value == "application.play").Text);
    [Fact] void should_keep_the_fragment_in_its_original_file() => _result.MoveReport.FragmentsMoved.Single().Document.ShouldEqual(Workspace.Documents.Single(document => document.Path.Value == "steps.play").Id);
}

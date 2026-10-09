// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_move_repairs_inherited_interaction_references : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("model", "application.play", """
            behavior GoHome
              parameter destination
              on enter
                navigate to destination
            module Projects
              on enter
                navigate to Projects.Source.Moving.Home
              uses GoHome
                destination Projects.Source.Moving.Home
              feature Source
                slice StateView Moving
                  screen Home
              feature Destination
                slice StateView Existing
                  screen Details
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForSlice(Workspace.IdentityCatalog.Application, "Projects", "Source", "Moving"),
        NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Destination"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_accept_the_unchanged_inherited_behavior() => Assert.True(_result.Accepted, string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_repair_both_inherited_references() => _result.MoveReport.ReferenceRepairs.Count(repair => repair.Previous == "Projects.Source.Moving.Home" && repair.Current == "Projects.Destination.Moving.Home").ShouldEqual(2);
}

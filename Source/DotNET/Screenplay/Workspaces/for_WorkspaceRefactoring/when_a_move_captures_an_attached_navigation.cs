// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_move_captures_an_attached_navigation : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("model", "application.play", """
            module Projects
              feature Source
                feature Moving
                  on enter
                    navigate to Home
                  slice StateView View
                    screen Details
                slice StateView Views
                  screen Home
              feature Destination
                slice StateView Views
                  screen Home
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", ["Source", "Moving"]),
        NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Destination"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_refuse_the_navigation_capture_in_the_moved_subtree() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_name_the_navigation_operand() => Assert.True(_result.Conflicts.Single().Message.Contains("Home", StringComparison.Ordinal) && _result.Conflicts.Single().Message.Contains("Reference capture", StringComparison.Ordinal), _result.Conflicts.Single().Message);
}

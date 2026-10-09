// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_move_captures_inherited_behavior_arguments : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects",
        [Document("model", "application.play", """
            behavior GoHome
              parameter destination
              on enter
                navigate to destination
            module Projects
              feature Source
                uses GoHome
                  destination Home
                slice StateView Views
                  screen Home
                slice StateView Moving
                  screen Details
              feature Destination
                uses GoHome
                  destination Home
                slice StateView Views
                  screen Home
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

    [Fact] void should_refuse_identical_arguments_that_resolve_to_different_targets() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_report_the_resolved_argument_targets() => Assert.True(_result.Conflicts.Single().Message.Contains("Projects.Source.Views.Home", StringComparison.Ordinal) && _result.Conflicts.Single().Message.Contains("Projects.Destination.Views.Home", StringComparison.Ordinal), _result.Conflicts.Single().Message);
}

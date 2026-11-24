// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_moving_a_slice_with_a_navigation_screen_target : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Shop", [Document("model", "application.play", when_renaming_a_navigation_screen_target.Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));
    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForSlice(Workspace.IdentityCatalog.Application, "Shop", "Projects", "Listing"),
        NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Shop", "Destination"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_refuse_the_partial_move() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_name_the_navigation_target() => _result.Conflicts.Single().Message.ShouldContain("navigation target");
}

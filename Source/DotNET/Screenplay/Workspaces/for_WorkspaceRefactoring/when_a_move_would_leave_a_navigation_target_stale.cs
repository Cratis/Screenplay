// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_move_would_leave_a_navigation_target_stale : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Shop",
        [Document("model", "application.play", """
            layout Shell
              content
              navigation sidebar
                item orders title "Orders" target Shop.Source.Moving

            module Shop
              feature Source
                feature Moving
              feature Destination
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));

    void Because() => _result = Workspace.ProposeMove(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Shop", ["Source", "Moving"]),
        NewParent = SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Shop", "Destination"),
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_refuse_the_move() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_say_a_navigation_item_targets_the_feature() => _result.Conflicts.Single().Message.ShouldContain("navigation item");
    [Fact] void should_never_offer_partial_writes() => _result.WritePlan.ShouldBeNull();
}

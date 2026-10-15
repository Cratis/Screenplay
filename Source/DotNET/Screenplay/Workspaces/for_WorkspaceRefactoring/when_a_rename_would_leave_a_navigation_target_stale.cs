// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_rename_would_leave_a_navigation_target_stale : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _module;
    WorkspaceAuthoringResult _feature;
    WorkspaceAuthoringResult _unrelated;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Shop",
        [Document("model", "application.play", """
            layout Shell
              content
              navigation sidebar
                item shop title "Shop" target Shop
                  item orders title "Orders" target Shop.Orders

            module Shop
              feature Orders
              feature Returns
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));

    void Because()
    {
        _module = Workspace.ProposeRename(Rename<ModuleSyntax>("Shop", "Store"));
        _feature = Workspace.ProposeRename(Rename<FeatureSyntax>("Orders", "Purchases"));
        _unrelated = Workspace.ProposeRename(Rename<FeatureSyntax>("Returns", "Refunds"));
    }

    [Fact] void should_refuse_renaming_a_targeted_module() => _module.Accepted.ShouldBeFalse();
    [Fact] void should_say_a_navigation_item_targets_the_module() => _module.Conflicts.Single().Message.ShouldContain("navigation item");
    [Fact] void should_refuse_renaming_a_targeted_feature() => _feature.Accepted.ShouldBeFalse();
    [Fact] void should_say_a_navigation_item_targets_the_feature() => _feature.Conflicts.Single().Message.ShouldContain("navigation item");
    [Fact] void should_still_rename_a_feature_no_item_targets() => _unrelated.Accepted.ShouldBeTrue();
}

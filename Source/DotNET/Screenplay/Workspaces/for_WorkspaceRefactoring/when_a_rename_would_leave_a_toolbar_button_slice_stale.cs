// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_rename_would_leave_a_toolbar_button_slice_stale : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _named;
    WorkspaceAuthoringResult _unrelated;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Shop",
        [Document("model", "application.play", """
            module Shop
              feature Orders
                slice StateChange PlaceOrder
                  command PlaceOrder
                    orderId Uuid identifier
                    produces OrderPlaced
                      orderId = orderId
                  event OrderPlaced
                    orderId Uuid
                slice StateChange CancelOrder
                  command CancelOrder
                    orderId Uuid identifier
                    produces OrderCancelled
                      orderId = orderId
                  event OrderCancelled
                    orderId Uuid
                slice StateView OrderList
                  readmodel OrderRow
                    orderId Uuid
                  screen List
                    toolbar actions
                      button place title "Place" slice Shop.Orders.PlaceOrder
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shop")));

    void Because()
    {
        _named = Workspace.ProposeRename(Rename<SliceSyntax>("PlaceOrder", "SubmitOrder"));
        _unrelated = Workspace.ProposeRename(Rename<SliceSyntax>("CancelOrder", "AbortOrder"));
    }

    [Fact] void should_refuse_renaming_the_slice_a_button_invokes() => _named.Accepted.ShouldBeFalse();
    [Fact] void should_say_a_toolbar_button_names_it() => _named.Conflicts.Single().Message.ShouldContain("toolbar button");
    [Fact] void should_still_rename_a_slice_no_button_invokes() => _unrelated.Accepted.ShouldBeTrue();
}

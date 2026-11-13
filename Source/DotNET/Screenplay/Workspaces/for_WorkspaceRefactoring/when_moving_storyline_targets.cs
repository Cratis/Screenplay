// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_moving_storyline_targets : given.a_refactoring_workspace
{
    const string Source = """
        concept ItemId : Uuid
        module Returns
          feature Requests
            slice StateChange RequestReturn
              command RequestReturn
                itemId ItemId
                produces ReturnRequested
                  itemId = itemId
              event ReturnRequested
                itemId ItemId
          feature Refunds
        module Archive
        storyline ReturnAnItem
          feature Returns.Requests
          slice Requests.RequestReturn
          command RequestReturn
        """;

    WorkspaceAuthoringResult _slice = null!;
    WorkspaceAuthoringResult _feature = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Returns", [Document("returns", "application.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Returns")));

    void Because()
    {
        var application = Workspace.IdentityCatalog.Application;
        _slice = Workspace.ProposeMove(Move(SemanticAddress.ForSlice(application, "Returns", "Requests", "RequestReturn"), SemanticAddress.ForFeature(application, "Returns", "Refunds")));
        _feature = Workspace.ProposeMove(Move(SemanticAddress.ForFeature(application, "Returns", "Requests"), SemanticAddress.ForModule(application, "Archive")));
    }

    [Fact] void should_accept_the_slice_move() => _slice.Conflicts.Select(conflict => conflict.Message).ShouldBeEmpty();
    [Fact] void should_rewrite_the_qualified_slice_step() => _slice.Workspace.Documents.Single().Text.ShouldContain("  slice Returns.Refunds.RequestReturn\n");
    [Fact] void should_leave_the_unqualified_command_step_alone() => _slice.Workspace.Documents.Single().Text.ShouldContain("  command RequestReturn\n");
    [Fact] void should_keep_every_step_resolved_after_the_slice_move() => _slice.Workspace.Compilation.Diagnostics.ShouldBeEmpty();
    [Fact] void should_accept_the_feature_move() => _feature.Conflicts.Select(conflict => conflict.Message).ShouldBeEmpty();
    [Fact] void should_rewrite_the_qualified_feature_step() => _feature.Workspace.Documents.Single().Text.ShouldContain("  feature Archive.Requests\n");
    [Fact] void should_keep_every_step_resolved_after_the_feature_move() => _feature.Workspace.Compilation.Diagnostics.ShouldBeEmpty();

    WorkspaceMoveRequest Move(SemanticAddress target, SemanticAddress parent) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = target,
        NewParent = parent,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_updating_a_slice_description_with_a_stale_revision : given.a_workspace_with_a_described_slice
{
    WorkspaceTransactionResult _workspaceResult = null!;
    WorkspaceTransactionResult _catalogResult = null!;

    void Because()
    {
        var request = Request(new UpdateSliceDescription
        {
            SemanticId = SliceId,
            ExpectedCurrentDescription = OriginalDescription,
            NewDescription = "Registers a brand new project"
        });
        _workspaceResult = Workspace.Propose(request with { ExpectedRevision = default });
        _catalogResult = Workspace.Propose(request with { ExpectedCatalogRevision = default });
    }

    [Fact] void should_reject_the_stale_workspace_revision_before_touching_the_operation() => _workspaceResult.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_reject_the_stale_catalog_revision_before_touching_the_operation() => _catalogResult.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
    [Fact] void should_return_no_candidate_workspace_for_either_conflict() => (_workspaceResult.Workspace is null && _catalogResult.Workspace is null).ShouldBeTrue();
    [Fact] void should_leave_the_original_document_untouched() => Workspace.Documents.Single(document => document.Id == Registration.Id).Text.ShouldEqual(RegistrationSource);
}

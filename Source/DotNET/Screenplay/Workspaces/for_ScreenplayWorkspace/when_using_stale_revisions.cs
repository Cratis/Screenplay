// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_using_stale_revisions : given.a_valid_workspace
{
    WorkspaceTransactionResult _workspaceResult = null!;
    WorkspaceTransactionResult _catalogResult = null!;

    void Because()
    {
        _workspaceResult = Workspace.Propose(Request() with { ExpectedRevision = default });
        _catalogResult = Workspace.Propose(Request() with { ExpectedCatalogRevision = default });
    }

    [Fact] void should_reject_the_stale_workspace_revision() => _workspaceResult.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_reject_the_stale_catalog_revision() => _catalogResult.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
    [Fact] void should_return_no_workspace_for_either_conflict() => new[] { _workspaceResult, _catalogResult }.All(result => result.Workspace is null).ShouldBeTrue();
    [Fact] void should_return_no_write_plan_for_either_conflict() => new[] { _workspaceResult, _catalogResult }.All(result => result.WritePlan is null).ShouldBeTrue();
    [Fact] void should_leave_the_original_workspace_unchanged() => Workspace.Documents.ShouldContainOnly(Concepts, Registration);
}

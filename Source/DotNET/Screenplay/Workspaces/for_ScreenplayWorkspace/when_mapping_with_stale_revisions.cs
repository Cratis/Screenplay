// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_with_stale_revisions : given.a_workspace_with_a_produced_mapping
{
    WorkspaceTransactionResult _workspaceResult = null!;
    WorkspaceTransactionResult _catalogResult = null!;

    void Because()
    {
        var malformed = Request(_operation with { Command = default }, _operation);
        _workspaceResult = _workspace.Propose(malformed with { ExpectedRevision = default });
        _catalogResult = _workspace.Propose(malformed with { ExpectedCatalogRevision = default });
    }

    [Fact] void should_gate_workspace_before_operation_admission() => _workspaceResult.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_gate_catalog_before_operation_admission() => _catalogResult.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
    [Fact] void should_offer_no_candidate() => new[] { _workspaceResult, _catalogResult }.All(result => result.Workspace is null).ShouldBeTrue();
    [Fact] void should_offer_no_write_plan() => new[] { _workspaceResult, _catalogResult }.All(result => result.WritePlan is null).ShouldBeTrue();
}
#endif

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_stale_revisions : given.a_property_chain_workspace
{
    WorkspaceAuthoringResult _catalog;

    void Because()
    {
        Result = Workspace.ProposeProperty(Request() with { ExpectedRevision = default });
        _catalog = Workspace.ProposeProperty(Request() with { ExpectedCatalogRevision = default });
    }

    [Fact] void should_gate_the_workspace_revision_first() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_gate_the_catalog_revision() => _catalog.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
    [Fact] void should_never_return_a_partial_plan() => Result.WritePlan.ShouldBeNull();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_stale_revisions : given.a_property_add_workspace
{
    WorkspaceAuthoringResult _catalog;

    void Because()
    {
        Result = Workspace.ProposeProperty(AddRequest() with { ExpectedRevision = default });
        _catalog = Workspace.ProposeProperty(AddRequest() with { ExpectedCatalogRevision = default });
    }

    [Fact] void should_gate_the_workspace_revision() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_gate_the_catalog_revision() => _catalog.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
    [Fact] void should_not_offer_partial_writes() => Result.WritePlan.ShouldBeNull();
}

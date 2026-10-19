// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property.with_an_identifier_payload_copy;

public class and_the_revisions_are_stale : given.an_identifier_payload_copy_workspace
{
    WorkspaceAuthoringResult _catalog = null!;

    void Establish() => Create(InlineSource);

    void Because()
    {
        Result = Workspace.ProposeProperty(Request() with { ExpectedRevision = default });
        _catalog = Workspace.ProposeProperty(Request() with { ExpectedCatalogRevision = default });
    }

    [Fact] void should_gate_the_workspace_revision() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_gate_the_catalog_revision() => _catalog.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
    [Fact] void should_not_plan_a_write() => Result.WritePlan.ShouldBeNull();
}

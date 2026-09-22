// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_rejecting_stale_authoring_requests : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _workspaceRevision = null!;
    WorkspaceAuthoringResult _catalogRevision = null!;
    WorkspaceAuthoringResult _handle = null!;

    void Because()
    {
        _workspaceRevision = Workspace.ProposeAuthoring(Authoring() with { ExpectedRevision = default });
        _catalogRevision = Workspace.ProposeAuthoring(Authoring() with { ExpectedCatalogRevision = default });
        _handle = Workspace.ProposeAuthoring(Authoring(new ReplaceWorkspaceNode(ConceptsRoot.Handle with { Revision = default }, ConceptsRoot.Node, ConceptsRoot.Node)));
    }

    [Fact] void should_gate_the_workspace_revision_first() => _workspaceRevision.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_gate_the_catalog_revision() => _catalogRevision.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
    [Fact] void should_reject_a_handle_from_another_snapshot() => _handle.Accepted.ShouldBeFalse();
    [Fact] void should_expose_no_partial_candidates() => new[] { _workspaceRevision, _catalogRevision, _handle }.All(result => result.Workspace is null).ShouldBeTrue();
}

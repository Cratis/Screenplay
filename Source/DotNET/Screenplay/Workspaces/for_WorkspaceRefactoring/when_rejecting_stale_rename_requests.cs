// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_rejecting_stale_rename_requests : given.a_refactoring_workspace
{
    WorkspaceRenameRequest _request = null!;

    void Establish() => _request = Rename<ConceptSyntax>("ProjectName", "ProjectTitle");

    [Fact] void should_reject_a_stale_revision() => Workspace.ProposeRename(_request with { ExpectedRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_reject_a_stale_catalog() => Workspace.ProposeRename(_request with { ExpectedCatalogRevision = default }).Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleCatalogRevision);
    [Fact] void should_reject_a_stale_handle() => Workspace.ProposeRename(_request with { Target = _request.Target with { Revision = default } }).Accepted.ShouldBeFalse();
    [Fact] void should_reject_a_drifted_expected_name() => Workspace.ProposeRename(_request with { ExpectedName = "Other" }).Accepted.ShouldBeFalse();
    [Fact] void should_not_relax_preserve_exact_source() => Workspace.ProposeRename(_request with { Formatting = WorkspaceAuthoringFormatting.PreserveExactSource }).Accepted.ShouldBeFalse();
}

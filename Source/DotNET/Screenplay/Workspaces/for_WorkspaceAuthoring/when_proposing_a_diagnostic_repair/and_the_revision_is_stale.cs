// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_proposing_a_diagnostic_repair;

public class and_the_revision_is_stale : given.a_legacy_validation
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request with { ExpectedRevision = default });

    [Fact] void should_report_a_typed_stale_conflict() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_not_provide_a_candidate() => _result.Workspace.ShouldBeNull();
    [Fact] void should_not_provide_a_write_plan() => _result.WritePlan.ShouldBeNull();
}

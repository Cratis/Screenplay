// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_inserting_new_debt : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringResult _draft = null!;
    WorkspaceAuthoringResult _stale = null!;

    void Because()
    {
        var request = Authoring(WorkspaceAuthoringReferencePolicy.Safe, RenameMember("part", "missing"));
        _safe = Workspace.ProposeAuthoring(request);
        _draft = Workspace.ProposeAuthoring(request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft });
        _stale = Workspace.ProposeAuthoring(request with { ExpectedRevision = default });
    }

    [Fact] void should_refuse_safe_new_debt() => _safe.Accepted.ShouldBeFalse();
    [Fact] void should_not_return_a_safe_write_plan() => _safe.WritePlan.ShouldBeNull();
    [Fact] void should_name_the_new_unresolved_key() => _safe.Conflicts.Single().Message.ShouldContain("New unresolved absence key 'missing'");
    [Fact] void should_admit_draft_new_debt() => _draft.WritePlan.ShouldNotBeNull();
    [Fact] void should_report_the_draft_debt() => AbsenceDebt(_draft).Single().ShouldContain("'missing'");
    [Fact] void should_refuse_stale_revision_first() => _stale.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
    [Fact] void should_not_return_a_stale_write_plan() => _stale.WritePlan.ShouldBeNull();
}

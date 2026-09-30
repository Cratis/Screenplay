// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_prepending_an_assertion_with_unresolved_key : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringResult _draft = null!;

    void Establish() => CreateWith(Source.Replace("\"part\":\"old\"", "\"missing\":\"old\"", StringComparison.Ordinal));

    void Because()
    {
        var request = Authoring(WorkspaceAuthoringReferencePolicy.Safe, PrependAssertion());
        _safe = Workspace.ProposeAuthoring(request);
        _draft = Workspace.ProposeAuthoring(request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft });
    }

    [Fact] void should_refuse_the_prepended_debt_under_safe() => _safe.Accepted.ShouldBeFalse();
    [Fact] void should_not_let_the_inserted_copy_borrow_the_existing_debt() => _safe.Conflicts.Single().Message.ShouldContain("/thenAbsentReadModels/0/");
    [Fact] void should_not_return_a_safe_write_plan() => _safe.WritePlan.ShouldBeNull();
    [Fact] void should_admit_the_prepended_debt_under_draft() => _draft.WritePlan.ShouldNotBeNull();
    [Fact] void should_report_both_unresolved_keys_under_draft() => AbsenceDebt(_draft).Count().ShouldEqual(2);
}

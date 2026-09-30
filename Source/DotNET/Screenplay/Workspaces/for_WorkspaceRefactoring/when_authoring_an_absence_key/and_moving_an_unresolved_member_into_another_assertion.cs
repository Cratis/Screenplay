// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_moving_an_unresolved_member_into_another_assertion : given.a_workspace_with_an_absent_read_model_key
{
    const string OtherAssertion = "then no readmodel InvoiceView for {\"id\":\"second\",\"detail\":{}}";

    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringResult _draft = null!;
    WorkspaceAuthoringResult _resolved = null!;

    void Because()
    {
        CreateWith(TwoAssertions("missing"));
        var request = MoveInto("missing");
        _safe = Workspace.ProposeAuthoring(request);
        _draft = Workspace.ProposeAuthoring(request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft });

        CreateWith(TwoAssertions("part"));
        _resolved = Workspace.ProposeAuthoring(MoveInto("part"));
    }

    [Fact] void should_refuse_safe_authoring_of_the_moved_debt() => _safe.Accepted.ShouldBeFalse();
    [Fact] void should_name_the_moved_debt() => _safe.Conflicts.Single().Message.ShouldContain("New unresolved absence key 'missing'");
    [Fact] void should_not_return_a_safe_write_plan() => _safe.WritePlan.ShouldBeNull();
    [Fact] void should_admit_the_moved_debt_as_draft() => _draft.WritePlan.ShouldNotBeNull();
    [Fact] void should_report_the_draft_debt() => AbsenceDebt(_draft).Single().ShouldContain("'missing'");
    [Fact] void should_accept_moving_a_resolved_member() => _resolved.Accepted.ShouldBeTrue();
    [Fact] void should_leave_no_debt_for_the_resolved_member() => AbsenceDebt(_resolved).ShouldBeEmpty();
    [Fact] void should_write_the_resolved_member_into_the_other_assertion() => Text(_resolved).ShouldContain("{\"id\":\"second\",\"detail\":{\"part\":\"old\"}}");

    static string TwoAssertions(string member) => Source
        .Replace("\"part\":\"old\"", $"\"{member}\":\"old\"", StringComparison.Ordinal) + $"\n        {OtherAssertion}";

    WorkspaceAuthoringRequest MoveInto(string member)
    {
        var moved = Member(member);
        var destination = Entry<ObjectExpressionSyntax>(expression => !expression.Members.Any());
        return Authoring(WorkspaceAuthoringReferencePolicy.Safe, new MoveWorkspaceNode(moved.Handle, moved.Node, destination.Handle, destination.Node, "members"));
    }
}

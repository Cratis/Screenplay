// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_adding_a_member_while_the_identifier_is_unresolved : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringResult _draft = null!;

    void Establish() => CreateWith(Source.Replace("by invoiceId InvoiceKey", "by missing InvoiceKey", StringComparison.Ordinal));

    void Because()
    {
        var key = Entry<ObjectExpressionSyntax>(expression => expression.Members.Any(member => member.Name == "id"));
        var typo = new ObjectMemberSyntax("typo", new LiteralExpressionSyntax("x", SourceLocation.Start), SourceLocation.Start);
        var request = Authoring(WorkspaceAuthoringReferencePolicy.Safe, new AddWorkspaceNode(key.Handle, key.Node, "members", typo));
        _safe = Workspace.ProposeAuthoring(request);
        _draft = Workspace.ProposeAuthoring(request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft });
    }

    [Fact] void should_refuse_the_new_member_under_safe() => _safe.Conflicts.Single().Message.ShouldContain("New unresolved absence key 'typo'");
    [Fact] void should_not_return_a_safe_write_plan() => _safe.WritePlan.ShouldBeNull();
    [Fact] void should_admit_it_under_draft() => _draft.WritePlan.ShouldNotBeNull();
    [Fact] void should_report_every_unresolved_obligation_under_draft() => AbsenceDebt(_draft).Any(message => message.Contains("'typo'", StringComparison.Ordinal)).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_repointing_an_assertion_with_unresolved_key : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringResult _draft = null!;

    void Establish() => CreateWith(Source
        .Replace(
            "      query InvoiceById => InvoiceView?",
            "      readmodel OtherView\n        invoiceId InvoiceKey\n      query OtherById => OtherView?\n        by invoiceId InvoiceKey\n      query InvoiceById => InvoiceView?",
            StringComparison.Ordinal)
        .Replace("\"part\":\"old\"", "\"missing\":\"old\"", StringComparison.Ordinal));

    void Because()
    {
        var assertion = Entry<SpecificationAbsentReadModelSyntax>(_ => true);
        var request = Authoring(
            WorkspaceAuthoringReferencePolicy.Safe,
            new ReplaceWorkspaceNode(assertion.Handle, assertion.Node, ((SpecificationAbsentReadModelSyntax)assertion.Node) with { Name = "OtherView" }));
        _safe = Workspace.ProposeAuthoring(request);
        _draft = Workspace.ProposeAuthoring(request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft });
    }

    [Fact] void should_refuse_safe_authoring_of_the_repointed_debt() => _safe.Accepted.ShouldBeFalse();
    [Fact] void should_name_the_repointed_debt() => _safe.Conflicts.Single().Message.ShouldContain("New unresolved absence key 'missing'");
    [Fact] void should_admit_the_repointed_debt_as_draft() => _draft.WritePlan.ShouldNotBeNull();
    [Fact] void should_report_the_draft_debt() => AbsenceDebt(_draft).Single().ShouldContain("'missing'");
}

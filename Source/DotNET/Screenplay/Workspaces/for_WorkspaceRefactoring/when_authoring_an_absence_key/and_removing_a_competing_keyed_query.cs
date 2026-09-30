// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_removing_a_competing_keyed_query : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith(Source.Replace(
        "        by invoiceId InvoiceKey",
        "        by invoiceId InvoiceKey\n      query InvoiceByOther => InvoiceView?\n        by other InvoiceKey",
        StringComparison.Ordinal));

    void Because()
    {
        var competing = Entry<QuerySyntax>(query => query.Name == "InvoiceByOther");
        _result = Workspace.ProposeAuthoring(Authoring(WorkspaceAuthoringReferencePolicy.Safe, new RemoveWorkspaceNode(competing.Handle, competing.Node)));
    }

    [Fact] void should_accept_the_explicit_identifier_repair() => _result.WritePlan.ShouldNotBeNull();
    [Fact] void should_remove_the_competing_query() => Text(_result).ShouldNotContain("InvoiceByOther");
    [Fact] void should_leave_no_absence_debt() => AbsenceDebt(_result).ShouldBeEmpty();
}

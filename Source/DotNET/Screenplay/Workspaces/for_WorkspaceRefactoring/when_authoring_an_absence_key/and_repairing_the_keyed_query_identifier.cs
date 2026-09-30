// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_repairing_the_keyed_query_identifier : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _before = null!;
    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith(Source.Replace("by invoiceId InvoiceKey", "by missing InvoiceKey", StringComparison.Ordinal));

    void Because()
    {
        var by = Entry<QueryParameterSyntax>(parameter => parameter.Name == "missing");
        var repair = new ReplaceWorkspaceNode(by.Handle, by.Node, ((QueryParameterSyntax)by.Node) with { Name = "invoiceId" });
        _before = Workspace.ProposeAuthoring(Authoring(WorkspaceAuthoringReferencePolicy.Draft, RenameMember("id", "id")));
        _result = Workspace.ProposeAuthoring(Authoring(WorkspaceAuthoringReferencePolicy.Safe, repair));
    }

    [Fact] void should_report_the_key_as_debt_while_the_identifier_is_unresolved() => AbsenceDebt(_before).ShouldNotBeEmpty();
    [Fact] void should_accept_the_explicit_identifier_repair() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_supply_a_write_plan() => _result.WritePlan.ShouldNotBeNull();
    [Fact] void should_write_the_repaired_identifier() => Text(_result).ShouldContain("by invoiceId InvoiceKey");
    [Fact] void should_leave_no_absence_debt() => AbsenceDebt(_result).ShouldBeEmpty();
}

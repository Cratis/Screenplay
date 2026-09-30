// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_replacing_a_document_that_keeps_unique_debt : given.a_workspace_with_an_absent_read_model_key
{
    const string Debt = "then no readmodel InvoiceView for {\"id\":\"first\",\"detail\":{\"missing\":\"old\"}}";

    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith(Source.Replace(Assertion, Debt, StringComparison.Ordinal));

    void Because() => _result = Workspace.ProposeAuthoring(ReplaceDocument(
        WorkspaceAuthoringReferencePolicy.Safe,
        $"type Unrelated\n  note String\n{Source.Replace(Assertion, Debt, StringComparison.Ordinal)}"));

    [Fact] void should_retain_the_uniquely_corresponding_debt_under_safe() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_write_the_replacement() => Text(_result).ShouldContain("type Unrelated");
    [Fact] void should_report_the_retained_debt() => AbsenceDebt(_result).Single().ShouldContain("'missing'");
}

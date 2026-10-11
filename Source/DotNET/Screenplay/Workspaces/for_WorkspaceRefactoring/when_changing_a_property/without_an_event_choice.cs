// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class without_an_event_choice : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request("Registered"));

    [Fact] void should_require_a_contract_decision() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.EventContractChangeRequiresDecision);
    [Fact] void should_not_expose_a_partial_workspace() => Result.Workspace.ShouldBeNull();
    [Fact] void should_report_no_accepted_planned_event_changes() => Result.PropertyReport.Generations.ShouldBeEmpty();
}

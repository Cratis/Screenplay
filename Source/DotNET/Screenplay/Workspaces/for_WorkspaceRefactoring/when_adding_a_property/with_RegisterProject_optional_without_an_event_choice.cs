// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_RegisterProject_optional_without_an_event_choice : given.a_RegisterProject_property_workspace
{
    void Because() => Result = Workspace.ProposeProperty(CorpusRequest() with { Optional = true });

    [Fact] void should_require_an_explicit_event_evolution_choice() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.EventContractChangeRequiresDecision);
    [Fact] void should_not_offer_partial_writes() => Result.WritePlan.ShouldBeNull();
    [Fact] void should_not_report_an_event_evolution() => Result.PropertyReport.Generations.ShouldBeEmpty();
}

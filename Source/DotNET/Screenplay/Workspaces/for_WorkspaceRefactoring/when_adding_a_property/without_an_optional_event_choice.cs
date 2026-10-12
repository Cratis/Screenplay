// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class without_an_optional_event_choice : given.a_property_add_workspace
{
    WorkspaceAuthoringResult _withoutSource;

    void Because()
    {
        Result = Workspace.ProposeProperty(AddRequest("Registered") with { Optional = true, Hops = [MappingHop(value: "\"Normal\"")] });
        _withoutSource = Workspace.ProposeProperty(AddRequest("Registered") with { Optional = true });
    }

    [Fact] void should_require_an_explicit_event_choice() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.EventContractChangeRequiresDecision);
    [Fact] void should_require_the_event_choice_even_without_a_source() => _withoutSource.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.EventContractChangeRequiresDecision);
    [Fact] void should_not_publish_changes() => Result.PropertyReport.Changed.ShouldBeEmpty();
    [Fact] void should_not_publish_generations() => Result.PropertyReport.Generations.ShouldBeEmpty();
}

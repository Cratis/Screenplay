// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_both_event_choices : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request("Registered") with { NewGeneration = true, EventNeverPersisted = true });

    [Fact] void should_reject_the_contradictory_request() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidOperation);
    [Fact] void should_not_expose_a_candidate() => Result.Workspace.ShouldBeNull();
}

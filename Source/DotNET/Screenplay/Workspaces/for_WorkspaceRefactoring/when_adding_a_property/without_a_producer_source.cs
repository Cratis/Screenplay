// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class without_a_producer_source : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest("Registered") with { EventNeverPersisted = true, Optional = true });

    [Fact] void should_require_a_source_decision() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.SourcesRequired);
    [Fact] void should_name_the_producer() => Result.Conflicts.Single().Candidates.Single().Role.ShouldEqual("mapping-target");
    [Fact] void should_be_atomic() => Result.Workspace.ShouldBeNull();
}

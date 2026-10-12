// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_a_hop_name_collision : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { Hops = [AddHop("Registered", "name")] });

    [Fact] void should_refuse_to_merge_the_hop() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidOperation);
    [Fact] void should_list_the_colliding_name() => Result.Conflicts.Single().Candidates.Single().Address.Name.ShouldEqual("name");
    [Fact] void should_keep_the_entire_plan_atomic() => Result.PropertyReport.Changed.ShouldBeEmpty();
}

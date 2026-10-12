// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class without_a_fixture_value : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest());

    [Fact] void should_require_every_command_fixture_value() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FixtureValueRequired);
    [Fact] void should_list_the_fixture_occurrence() => Result.Conflicts.Single().Candidates.Single().Role.ShouldEqual("fixture");
    [Fact] void should_have_no_candidate_workspace() => Result.Workspace.ShouldBeNull();
    [Fact] void should_have_no_changed_entries() => Result.PropertyReport.Changed.ShouldBeEmpty();
    [Fact] void should_have_no_generation_entries() => Result.PropertyReport.Generations.ShouldBeEmpty();
    [Fact] void should_have_no_write_plan() => Result.WritePlan.ShouldBeNull();
}

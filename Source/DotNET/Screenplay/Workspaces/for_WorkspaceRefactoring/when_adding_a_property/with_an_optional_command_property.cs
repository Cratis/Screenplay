// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_an_optional_command_property : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { Optional = true });

    [Fact] void should_accept_without_fixture_values() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_not_add_fixture_values() => Result.PropertyReport.Changed.Any(change => change.Change == "fixtureAdded").ShouldBeFalse();
    [Fact] void should_have_no_event_evolutions() => Result.PropertyReport.Generations.ShouldBeEmpty();
}

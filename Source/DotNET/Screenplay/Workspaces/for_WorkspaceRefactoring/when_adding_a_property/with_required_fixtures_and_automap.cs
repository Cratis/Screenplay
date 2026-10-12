// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_required_fixtures_and_automap : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { EventNeverPersisted = true, FixtureValue = "\"Normal\"", Hops = [AddHop("Registered"), MappingHop(), AddHop("Project")] });

    [Fact] void should_accept_the_explicit_chain() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_add_three_properties() => Result.PropertyReport.Changed.Count(change => change.Role == "declaration").ShouldEqual(3);
    [Fact] void should_add_command_and_event_fixtures() => Result.PropertyReport.Changed.Count(change => change.Change == "fixtureAdded").ShouldEqual(2);
    [Fact] void should_report_the_in_place_event_change() => Result.PropertyReport.Generations.Single().Mode.ShouldEqual("eventNeverPersisted");
    [Fact] void should_leave_the_unselected_event_unchanged() => Candidate.Contains("event Audited generation 2\n        name String\n        priority", StringComparison.Ordinal).ShouldBeFalse();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_RegisterProject_never_persisted : given.a_RegisterProject_property_workspace
{
    void Because() => Result = Workspace.ProposeProperty(CorpusRequest() with { EventNeverPersisted = true });

    [Fact] void should_bind_the_candidate() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_pass_every_model_specification() => Runs().SelectMany(run => run.Failures).ShouldBeEmpty();
    [Fact] void should_add_to_generation_two() => Events.Single(@event => @event.Generation == 2).Properties.Any(property => property.Name == "priority").ShouldBeTrue();
    [Fact] void should_leave_generation_one_untouched() => Events.Single(@event => @event.Generation == 1).Properties.Any(property => property.Name == "priority").ShouldBeFalse();
    [Fact] void should_disclose_the_in_place_revision() => Result.PropertyReport.Generations.Single().From.ShouldEqual(Result.PropertyReport.Generations.Single().To);
    [Fact] void should_add_every_fixture_and_assertion() => Result.PropertyReport.Changed.Count(change => change.Change == "fixtureAdded").ShouldEqual(5);
}

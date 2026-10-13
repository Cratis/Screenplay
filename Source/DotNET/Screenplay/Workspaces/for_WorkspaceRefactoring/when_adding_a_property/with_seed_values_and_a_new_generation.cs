// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_seed_values_and_a_new_generation : given.a_seed_property_workspace
{
    void Establish() => Workspace = Create(SeedSource.Replace("event Registered\n        name String", "event Registered generation 1\n        name String\n      event Registered generation 2\n        name String", StringComparison.Ordinal));
    void Because() => Result = Workspace.ProposeProperty(AddRequest("Registered", 2) with { NewGeneration = true, FixtureValue = "\"Normal\"" });

    [Fact] void should_accept() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_fill_every_current_seed_entry() => RegisteredSeeds.All(seed => seed.Properties.Any(property => property.Property == "priority")).ShouldBeTrue();
    [Fact] void should_leave_historical_shapes_without_the_added_property() => WorkspaceSyntaxIndex.Create(Result.Workspace!).Entries.Select(entry => entry.Node).OfType<EventSyntax>().Where(@event => @event.Name == "Registered" && @event.Generation < 3).SelectMany(@event => @event.Properties).Select(property => property.Name).ShouldContainOnly("name", "name");
    [Fact] void should_advance_the_current_generation() => Result.PropertyReport.Generations.Single().From.ShouldEqual(2u);
    [Fact] void should_report_the_new_generation() => Result.PropertyReport.Generations.Single().To.ShouldEqual(3u);
}

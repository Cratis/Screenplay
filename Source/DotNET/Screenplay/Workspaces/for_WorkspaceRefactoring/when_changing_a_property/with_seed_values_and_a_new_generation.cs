// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_seed_values_and_a_new_generation : given.a_seed_property_workspace
{
    void Establish() => Workspace = Create(SeedSource.Replace("event Registered\n        name String", "event Registered generation 1\n        name String\n      event Registered generation 2\n        name String", StringComparison.Ordinal));
    void Because() => Result = Workspace.ProposeProperty(Request("Registered", generation: 2) with { NewGeneration = true });

    [Fact] void should_accept() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_rewrite_current_seed_assignments() => RegisteredSeeds.SelectMany(seed => seed.Properties).Select(property => property.Property).ShouldContainOnly("label", "label");
    [Fact] void should_keep_historical_shapes() => WorkspaceSyntaxIndex.Create(Result.Workspace!).Entries.Select(entry => entry.Node).OfType<EventSyntax>().Where(@event => @event.Name == "Registered" && @event.Generation < 3).SelectMany(@event => @event.Properties).Select(property => property.Name).ShouldContainOnly("name", "name");
    [Fact] void should_advance_the_current_generation() => Result.PropertyReport.Generations.Single().From.ShouldEqual(2u);
    [Fact] void should_report_the_new_generation() => Result.PropertyReport.Generations.Single().To.ShouldEqual(3u);
}

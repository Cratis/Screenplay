// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_a_new_initial_event_generation : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { NewGeneration = true, FixtureValue = "\"Normal\"", Hops = [AddHop("Registered"), MappingHop(), AddHop("Project")] });

    [Fact] void should_accept_the_plan() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_preserve_the_original_shape() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Select(entry => entry.Node).OfType<EventSyntax>().Single(@event => @event.Name == "Registered" && @event.Generation == 1).Properties.Select(property => property.Name).ShouldContainOnly("name");
    [Fact] void should_create_a_full_second_generation() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Select(entry => entry.Node).OfType<EventSyntax>().Single(@event => @event.Name == "Registered" && @event.Generation == 2).Properties.Select(property => property.Name).ShouldContainOnly("name", "priority");
    [Fact] void should_keep_existing_command_property_identity() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Single(entry => entry.Handle.Path == Property("Register").Handle.Path).SemanticId.ShouldEqual(Property("Register").SemanticId);
}

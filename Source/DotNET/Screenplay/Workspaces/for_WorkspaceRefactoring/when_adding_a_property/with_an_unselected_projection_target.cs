// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_an_unselected_projection_target : given.a_property_add_workspace
{
    void Establish() => Workspace = Create(Source.Replace("readmodel Project\n        projectId String", "readmodel Project\n        priority String\n        projectId String", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { EventNeverPersisted = true, FixtureValue = "\"Normal\"", Hops = [AddHop("Registered"), MappingHop()] });

    [Fact] void should_not_enable_a_write_to_an_unselected_target() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.AmbiguousPropertyReference);
    [Fact] void should_not_publish_the_new_properties() => Result.PropertyReport.Changed.ShouldBeEmpty();
    [Fact] void should_not_offer_partial_writes() => Result.WritePlan.ShouldBeNull();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class without_a_consumer_source : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest("Project") with { Optional = true });

    [Fact] void should_require_a_projection_decision() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.SourcesRequired);
    [Fact] void should_list_the_feed() => Result.Conflicts.Single().Candidates.Single().Role.ShouldEqual("automap");
}

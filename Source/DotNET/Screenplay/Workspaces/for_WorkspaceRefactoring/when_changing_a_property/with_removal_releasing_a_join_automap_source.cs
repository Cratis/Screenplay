// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_releasing_a_join_automap_source : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource(JoinSource);

    void Because() => Result = Workspace.ProposeProperty(Request("View", "remove", name: "value"));

    [Fact] void should_refuse_the_new_writer_into_the_unselected_member() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.AmbiguousPropertyReference);
    [Fact] void should_return_original_source_locations() => Result.Conflicts.Single().Candidates.All(candidate => candidate.Location.Path == "model.play" && candidate.Location.Line == 13).ShouldBeTrue();
    [Fact] void should_not_expose_a_partial_workspace() => Result.Workspace.ShouldBeNull();
}

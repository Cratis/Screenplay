// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_of_a_suppressed_join_writer : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource(JoinSource.Replace("projection View => View", "projection View => View\n        no automap", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("View", "remove", name: "value"));

    [Fact] void should_accept_removal_without_releasing_automap() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_remove_only_the_selected_writer() => Candidate.Contains("value = name", StringComparison.Ordinal).ShouldBeFalse();
}

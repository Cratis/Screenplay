// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_removal_of_a_suppressed_aggregate_writer : given.a_projection_effect_workspace
{
    void Establish() => UseBindableSource(AggregateSource.Replace("projection View => View", "projection View => View\n        no automap", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("View", "remove", name: "count"));

    [Fact] void should_accept_removal_without_new_effects() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_keep_the_unselected_view_member() => Candidate.Contains("name String", StringComparison.Ordinal).ShouldBeTrue();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_it_would_introduce_an_unmet_view_obligation : given.a_workspace_with_two_slices_to_move_between
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeMove(Request(SemanticAddress.ForProjection(Slice("Source", "Listing"), "ProjectList"), Slice("Destination", "Lookup")));

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_name_the_unmet_view_obligation() => Refusals(_result).ShouldContain("SPEC006 of ReadModel 'Projects.Destination.Lookup.ProjectView' (view)");
    [Fact] void should_return_no_candidate() => _result.Workspace.ShouldBeNull();
}

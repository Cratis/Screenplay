// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_supplied_specifications_match_coverage : given.a_workspace_with_two_slices_to_move_between
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeMove(Request(SemanticAddress.ForCommand(Slice("Source", "Registration"), "Register"), Slice("Destination", "Intake"), SemanticAddress.ForSpecification(Slice("Source", "Registration"), "Registering")));

    [Fact] void should_accept_the_compatible_request() => Assert.True(_result.Accepted, Refusals(_result));
    [Fact] void should_report_the_computed_specification() => _result.MoveReport!.SpecificationsCarried.ShouldContainOnly(SemanticAddress.ForSpecification(Slice("Destination", "Intake"), "Registering"));
}

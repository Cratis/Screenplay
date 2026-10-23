// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_supplied_specifications_disagree_with_coverage : given.a_workspace_with_two_slices_to_move_between
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeMove(Request(SemanticAddress.ForCommand(Slice("Source", "Registration"), "Register"), Slice("Destination", "Intake"), SemanticAddress.ForSpecification(Slice("Source", "Registration"), "Renaming")));

    [Fact] void should_refuse() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_explain_that_carry_is_computed() => Refusals(_result).ShouldContain("Caller-supplied carried specifications must equal the specifications computed for this declaration move.");
    [Fact] void should_return_no_candidate() => _result.Workspace.ShouldBeNull();
}

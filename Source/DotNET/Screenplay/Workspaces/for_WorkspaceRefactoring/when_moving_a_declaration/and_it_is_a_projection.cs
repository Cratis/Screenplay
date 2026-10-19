// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_it_is_a_projection : given.a_workspace_with_two_slices_to_move_between
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeMove(Request(SemanticAddress.ForProjection(Slice("Source", "Listing"), "ProjectList"), Slice("Destination", "Lookup")));

    [Fact] void should_accept() => Assert.True(_result.Accepted, Refusals(_result));
    [Fact] void should_keep_the_model_executable() => Assert.True(_result.Workspace!.Compilation.Success, CompilationDiagnostics(_result.Workspace));
    [Fact] void should_place_the_projection_in_the_destination_slice() => SliceNamed(_result, "Lookup").Projections.Select(projection => projection.Name).ShouldContainOnly("ProjectList");
    [Fact] void should_leave_the_read_model_it_builds() => (SliceNamed(_result, "Listing").ReadModels ?? []).Select(model => model.Name).ShouldContainOnly("ProjectView");
}

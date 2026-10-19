// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_it_is_refused : given.a_workspace_with_two_slices_to_move_between
{
    protected override string DestinationMembers => """
              command Register
                projectId ProjectId identifier
                produces ProjectRegistered
                  for projectId
        """;

    SemanticAddress Source => Slice("Source", "Registration");

    string Refusal(WorkspaceMoveRequest request) => Workspace.ProposeMove(request).Conflicts.Single().Message;

    [Fact] void should_refuse_a_destination_collision() => Refusal(Request(SemanticAddress.ForCommand(Source, "Register"), Slice("Destination", "Intake"))).ShouldContain("Destination collision");
    [Fact] void should_refuse_a_specification_target() => Refusal(Request(SemanticAddress.ForSpecification(Source, "Renaming"), Slice("Destination", "Intake"))).ShouldContain("moves only with the declaration it covers");
    [Fact] void should_refuse_a_feature_destination() => Refusal(Request(SemanticAddress.ForCommand(Source, "Rename"), SemanticAddress.ForFeature(Workspace.IdentityCatalog.Application, "Projects", "Destination"))).ShouldContain("destination must be a slice");
    [Fact] void should_refuse_a_no_op() => Refusal(Request(SemanticAddress.ForCommand(Source, "Rename"), Source)).ShouldContain("no-op");
    [Fact] void should_refuse_a_carried_declaration_that_is_not_a_specification() => Refusal(Request(SemanticAddress.ForCommand(Source, "Rename"), Slice("Destination", "Intake"), SemanticAddress.ForCommand(Source, "Register"))).ShouldContain("is not a specification");
}

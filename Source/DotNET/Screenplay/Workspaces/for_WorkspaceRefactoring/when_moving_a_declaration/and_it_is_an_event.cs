// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_it_is_an_event : given.a_workspace_with_two_slices_to_move_between
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = Workspace.ProposeMove(Request(SemanticAddress.ForEventContract(Slice("Source", "Registration"), "ProjectRenamed"), Slice("Destination", "Intake")));

    [Fact] void should_accept() => Assert.True(_result.Accepted, Refusals(_result));
    [Fact] void should_keep_the_model_executable() => Assert.True(_result.Workspace!.Compilation.Success, CompilationDiagnostics(_result.Workspace));
    [Fact] void should_place_the_event_in_the_destination_slice() => SliceNamed(_result, "Intake").Events.Select(@event => @event.Name).ShouldContainOnly("ProjectRenamed");
    [Fact] void should_leave_the_command_that_produces_it() => SliceNamed(_result, "Registration").Commands.Select(command => command.Name).ShouldContainOnly("Register", "Rename");
    [Fact] void should_carry_no_specification() => _result.MoveReport!.SpecificationsCarried.ShouldBeEmpty();
    [Fact] void should_migrate_the_event_contract() => _result.MoveReport!.IdentityMigrations.Where(migration => migration.Domain == "event").Select(migration => migration.CurrentAddress).ShouldContain(SemanticAddress.ForEventContract(Slice("Destination", "Intake"), "ProjectRenamed"));
    [Fact] void should_keep_every_event_contract_identity() => _result.Workspace!.IdentityCatalog.EventContracts.Select(assignment => assignment.Id).ShouldContainOnly(Workspace.IdentityCatalog.EventContracts.Select(assignment => assignment.Id));
}

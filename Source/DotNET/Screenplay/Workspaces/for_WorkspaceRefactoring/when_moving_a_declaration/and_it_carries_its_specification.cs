// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration;

public class and_it_carries_its_specification : given.a_workspace_with_two_slices_to_move_between
{
    WorkspaceAuthoringResult _result = null!;
    SemanticAddress _moved = null!;

    void Because()
    {
        var source = Slice("Source", "Registration");
        _moved = SemanticAddress.ForCommand(Slice("Destination", "Intake"), "Register");
        _result = Workspace.ProposeMove(Request(SemanticAddress.ForCommand(source, "Register"), Slice("Destination", "Intake")));
    }

    WorkspaceSyntaxEntry[] Entries => [.. WorkspaceSyntaxIndex.Create(_result.Workspace).Entries];

    SliceSyntax SliceNamed(string name) => Entries.Select(entry => entry.Node).OfType<SliceSyntax>().Single(slice => slice.Name == name);

    [Fact] void should_keep_the_model_executable() => Assert.True(_result.Workspace!.Compilation.Success, CompilationDiagnostics(_result.Workspace));
    [Fact] void should_accept() => Assert.True(_result.Accepted, string.Join(" | ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_place_the_command_in_the_destination_slice() => SliceNamed("Intake").Commands.Select(command => command.Name).ShouldContainOnly("Register");
    [Fact] void should_carry_its_specification() => SliceNamed("Intake").Specifications.Select(specification => specification.Name).ShouldContainOnly("Registering");
    [Fact] void should_leave_the_staying_command_and_specification() => SliceNamed("Registration").Specifications.Select(specification => specification.Name).ShouldContainOnly("Renaming");
    [Fact] void should_leave_the_produced_events_behind() => SliceNamed("Registration").Events.Select(@event => @event.Name).ShouldContainOnly("ProjectRegistered", "ProjectRenamed");
    [Fact] void should_repair_the_screen_action_that_executes_it() => Entries.Select(entry => entry.Node).OfType<ExecuteCommandActionSyntax>().Single().Command.ShouldEqual("Projects.Destination.Intake.Register");
    [Fact] void should_report_the_carried_specification() => _result.MoveReport.SpecificationsCarried.ShouldContainOnly(SemanticAddress.ForSpecification(Slice("Destination", "Intake"), "Registering"));
    [Fact] void should_report_the_moved_occurrences() => _result.MoveReport.FragmentsMoved.Length.ShouldEqual(2);
    [Fact] void should_migrate_the_command_identity() => _result.MoveReport.IdentityMigrations.Where(migration => migration.Domain == "semantic").Select(migration => migration.CurrentAddress).ShouldContain(_moved);
    [Fact] void should_keep_every_semantic_identity() => _result.Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id).ShouldContainOnly(Workspace.IdentityCatalog.Semantics.Select(assignment => assignment.Id));
    [Fact] void should_leave_every_event_contract_untouched() => _result.Workspace.IdentityCatalog.EventContracts.ShouldContainOnly(Workspace.IdentityCatalog.EventContracts);
    [Fact] void should_retire_nothing() => _result.MoveReport.Retired.ShouldBeEmpty();
}

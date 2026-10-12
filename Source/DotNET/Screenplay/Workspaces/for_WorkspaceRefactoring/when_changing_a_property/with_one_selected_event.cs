// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_one_selected_event : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request("Registered") with { NewGeneration = true });

    [Fact] void should_accept() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_report_one_advancement() => Result.PropertyReport.Generations.Single().To.ShouldEqual(2u);
    [Fact] void should_keep_the_connected_command_field() => Candidate.Contains("name = name", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_current_event_contract_identity() => Result.Workspace.IdentityCatalog.ResolveEventContract(Result.PropertyReport.Generations.Single().Event).Id.ShouldEqual(Workspace.IdentityCatalog.ResolveEventContract(Result.PropertyReport.Generations.Single().Event).Id);
}

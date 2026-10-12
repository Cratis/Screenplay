// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class and_removal_has_only_writers_and_fixtures : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request("Audited", "remove", 2) with { EventNeverPersisted = true });

    [Fact] void should_accept() => Result.Conflicts.Select(conflict => conflict.Message).ShouldBeEmpty();
    [Fact] void should_disclose_the_removed_expected_value() => Result.PropertyReport.Changed.Single(change => change.Role == "assertion").WeakensAssertion.ShouldBeTrue();
    [Fact] void should_report_unused_inputs_without_removing_them() => Result.PropertyReport.ChainCandidates.Any(candidate => candidate.Kind == "Command").ShouldBeTrue();
    [Fact] void should_explicitly_retire_only_the_selected_identity() => Result.Workspace.IdentityCatalog.Semantics.Any(assignment => assignment.Address.Equals(Property("Audited", generation: 2).Address)).ShouldBeFalse();
    [Fact] void should_keep_the_old_generation_identity() => Result.Workspace.IdentityCatalog.ResolveSemanticAssignment(Property("Audited", generation: 1).Address).Id.ShouldEqual(Property("Audited", generation: 1).SemanticId!.Value);
}

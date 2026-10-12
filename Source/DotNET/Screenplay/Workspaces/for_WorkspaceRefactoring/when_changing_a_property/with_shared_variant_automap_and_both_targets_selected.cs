// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_shared_variant_automap_and_both_targets_selected : given.a_shared_variant_automap_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request("One") with { AlsoRename = [Select("Two")] });

    [Fact] void should_accept_every_selected_variant_consumer() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_insert_exactly_one_physical_mapping() => InsertedMappings.ShouldEqual(1);
    [Fact] void should_preserve_both_effective_flows() => Flows.ShouldEqual(ExpectedFlows("label", "name"));
    [Fact] void should_disclose_the_mapping_for_both_affected_properties() => Result.PropertyReport.Changed.Where(change => change.Change == "explicitMappingInserted").Select(change => change.Address).ShouldEqual(new[] { Property("One").Address, Property("Two").Address });
    [Fact] void should_not_change_an_event_contract() => Result.PropertyReport.Generations.ShouldBeEmpty();
}

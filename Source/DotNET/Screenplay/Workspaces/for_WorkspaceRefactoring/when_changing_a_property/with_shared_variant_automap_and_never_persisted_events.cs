// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_shared_variant_automap_and_never_persisted_events : given.a_shared_variant_automap_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request("Changed") with { EventNeverPersisted = true });

    [Fact] void should_accept_the_safe_shared_mapping() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_insert_exactly_one_physical_mapping() => InsertedMappings.ShouldEqual(1);
    [Fact] void should_preserve_both_effective_flows() => Flows.ShouldEqual(ExpectedFlows("name", "label"));
    [Fact] void should_preserve_every_variant_field_name_and_identity() => VariantProperties(Result.Workspace).ShouldEqual(VariantProperties(Workspace));
    [Fact] void should_report_the_in_place_event_edit() => Result.PropertyReport.Generations.Select(entry => (entry.Event.Name, entry.From, entry.To, entry.Mode)).ShouldEqual(new[] { ("Changed", 1u, 1u, "eventNeverPersisted") });
    [Fact] void should_disclose_the_inserted_mapping() => Result.PropertyReport.Changed.Count(change => change.Change == "explicitMappingInserted" && change.Address.Equals(Property("Changed").Address)).ShouldEqual(1);
    [Fact] void should_preserve_the_renamed_event_property_identity() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Single(entry => entry.Node is PropertySyntax property && property.Name == "label").SemanticId.ShouldEqual(Property("Changed").SemanticId);
}

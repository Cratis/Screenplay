// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_shared_variant_automap_and_a_new_generation : given.a_shared_variant_automap_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request("Changed") with { NewGeneration = true });

    [Fact] void should_accept_the_safe_shared_mapping() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_insert_exactly_one_physical_mapping() => InsertedMappings.ShouldEqual(1);
    [Fact] void should_preserve_both_effective_flows() => Flows.ShouldEqual(ExpectedFlows("name", "label"));
    [Fact] void should_preserve_every_variant_field_name_and_identity() => VariantProperties(Result.Workspace).ShouldEqual(VariantProperties(Workspace));
    [Fact] void should_report_one_event_advancement() => Result.PropertyReport.Generations.Select(entry => (entry.Event.Name, entry.From, entry.To, entry.Mode)).ShouldEqual(new[] { ("Changed", 1u, 2u, "newGeneration") });
    [Fact] void should_disclose_the_inserted_mapping() => Result.PropertyReport.Changed.Count(change => change.Change == "explicitMappingInserted" && change.Address.Equals(Property("Changed").Address)).ShouldEqual(1);
    [Fact] void should_preserve_the_historical_event_field() => ((EventSyntax)WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Single(entry => entry.Node is EventSyntax { Name: "Changed", Generation: 1 }).Node).Properties.Select(property => (property.Name, property.Type.Name)).ShouldEqual(new[] { ("name", "String") });
    [Fact] void should_assign_a_new_identity_to_the_new_generation_property() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Single(entry => entry.Node is PropertySyntax property && property.Name == "label").SemanticId.ShouldNotEqual(Property("Changed").SemanticId);
    [Fact] void should_preserve_the_historical_property_identity() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Single(entry => entry.Node is PropertySyntax property && property.Name == "name" && entry.Address?.OwnerKind == Semantics.SemanticKind.EventContract).SemanticId.ShouldEqual(Property("Changed").SemanticId);
}

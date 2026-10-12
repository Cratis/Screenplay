// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_all_three_explicitly_selected_hops : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request() with { EventNeverPersisted = true, AlsoRename = [Select("Registered"), Select("Project")] });

    [Fact] void should_accept_the_resolved_chain() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_keep_automap_without_inserting_an_unnecessary_mapping() => Result.PropertyReport.Changed.Any(change => change.Change == "explicitMappingInserted").ShouldBeFalse();
    [Fact] void should_change_only_the_selected_event_contract() => Result.PropertyReport.Generations.Single().Event.Name.ShouldEqual("Registered");
    [Fact] void should_keep_the_read_model_property_identity() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Single(entry => entry.Address?.OwnerKind == Semantics.SemanticKind.ReadModel && entry.Address.Name == "label").SemanticId.ShouldEqual(Property("Project").SemanticId);
}

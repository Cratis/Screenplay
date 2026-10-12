// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_two_selected_events : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request() with { NewGeneration = true, AlsoRename = [Select("Audited", 2), Select("Registered"), Select("Audited", 2)] });

    [Fact] void should_accept_the_entire_plan() => Result.Conflicts.Select(conflict => conflict.Message).ShouldBeEmpty();
    [Fact] void should_report_each_owners_own_revision_pair() => Result.PropertyReport.Generations.Select(entry => (entry.Event.Name, entry.From, entry.To, entry.Mode)).ShouldEqual(new[] { ("Audited", 2u, 3u, "newGeneration"), ("Registered", 1u, 2u, "newGeneration") });
    [Fact] void should_keep_all_historical_shapes() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Count(entry => entry.Node is EventSyntax @event && @event.Properties.Any(property => property.Name == "name")).ShouldEqual(4);
    [Fact] void should_advance_each_selected_catalog_event_once() => Result.PropertyReport.Generations.All(entry => Result.Workspace.IdentityCatalog.ResolveEventContract(entry.Event).Revision.Value == entry.To).ShouldBeTrue();
    [Fact] void should_disclose_the_transformation_requirement() => Result.PropertyReport.DeployableWithoutTransformation.ShouldBeFalse();
    [Fact] void should_insert_an_explicit_mapping_for_the_unselected_view() => Candidate.Contains("name = label", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_current_and_historical_property_identities_distinct() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Where(entry => entry.Node is PropertySyntax && entry.Address?.OwnerKind == Semantics.SemanticKind.EventContract).Select(entry => entry.SemanticId).Distinct().Count().ShouldEqual(6);
}

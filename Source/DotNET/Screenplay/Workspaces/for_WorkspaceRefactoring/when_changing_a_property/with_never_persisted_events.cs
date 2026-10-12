// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_never_persisted_events : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request() with { EventNeverPersisted = true, AlsoRename = [Select("Registered"), Select("Audited", 2)] });

    [Fact] void should_accept() => Result.Conflicts.Select(conflict => conflict.Message).ShouldBeEmpty();
    [Fact] void should_report_in_place_edits_in_canonical_order() => Result.PropertyReport.Generations.Select(entry => (entry.Event.Name, entry.From, entry.To, entry.Mode)).ShouldEqual(new[] { ("Audited", 2u, 2u, "eventNeverPersisted"), ("Registered", 1u, 1u, "eventNeverPersisted") });
    [Fact] void should_keep_the_current_event_property_identity() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Single(entry => entry.Node is PropertySyntax property && property.Name == "label" && entry.Address?.Parts.Any(part => part.Key == "Audited") == true).SemanticId.ShouldEqual(Property("Audited", generation: 2).SemanticId);
    [Fact] void should_leave_generation_one_unchanged() => Candidate.Contains("event Audited generation 1\n        name String", StringComparison.Ordinal).ShouldBeTrue();
}

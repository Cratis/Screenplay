// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class and_only_the_command_is_selected : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_accept() => Result.Conflicts.Select(conflict => conflict.Message).ShouldBeEmpty();
    [Fact] void should_rewrite_every_production_mapping() => Candidate.Split("name = label", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_keep_the_unselected_event_names() => Candidate.Contains("event Registered\n        name String", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_no_selected_event_edits() => Result.PropertyReport.Generations.ShouldBeEmpty();
    [Fact] void should_disclose_same_named_off_chain_properties() => Result.PropertyReport.NotChanged.Length.ShouldEqual(3);
    [Fact] void should_rewrite_the_command_fixture() => Candidate.Contains("label = \"Ada\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_the_command_property_identity() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Single(entry => entry.Node is Syntax.PropertySyntax property && property.Name == "label").SemanticId.ShouldEqual(Workspace.IdentityCatalog.ResolveSemanticAssignment(Property("Register").Address).Id);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_a_same_named_off_chain_property : given.a_property_add_workspace
{
    void Establish() => Workspace = Create(Source.Replace("command Unrelated\n        name String", "command Unrelated\n        priority String", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { Optional = true });

    [Fact] void should_accept_only_the_selected_declaration() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_disclose_the_off_chain_property() => Result.PropertyReport.NotChanged.Single().Reason.ShouldEqual("offChain");
    [Fact] void should_leave_the_off_chain_property_required() => WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Select(entry => entry.Node).OfType<CommandSyntax>().Single(command => command.Name == "Unrelated").Properties.Single().Type.IsOptional.ShouldBeFalse();
}

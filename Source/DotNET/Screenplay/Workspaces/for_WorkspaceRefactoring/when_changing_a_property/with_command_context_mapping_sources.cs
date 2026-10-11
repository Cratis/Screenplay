// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_command_context_mapping_sources : given.a_property_chain_workspace
{
    void Establish() => Workspace = Create(Source.Replace("name = name", "name = $context.command.name", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_accept_the_indexed_context_reader() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_preserve_the_context_prefix() => Candidate.Split("name = $context.command.label", StringSplitOptions.None).Length.ShouldEqual(3);
    [Fact] void should_not_select_the_connected_events() => Result.PropertyReport.Generations.ShouldBeEmpty();
}

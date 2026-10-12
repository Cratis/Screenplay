// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_compatible_shared_automap_sources : given.a_property_chain_workspace
{
    void Establish() => Workspace = Create(Source.Replace("from Registered", "all", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("Project"));

    [Fact] void should_accept_a_mapping_with_the_same_proven_source_for_every_feed() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_insert_only_one_shared_mapping() => Candidate.Split("label = name", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_not_select_any_event_contract() => Result.PropertyReport.Generations.ShouldBeEmpty();
}

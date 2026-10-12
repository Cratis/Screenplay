// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class without_a_selected_mapping_target : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { Optional = true, Hops = [MappingHop()] });

    [Fact] void should_require_an_explicit_target_property_hop() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.SourcesRequired);
    [Fact] void should_identify_the_undecided_mapping_block() => Result.Conflicts.Single().Candidates.Single().Location.ShouldEqual(Production().Location);
    [Fact] void should_not_implicitly_add_an_event_property() => Result.PropertyReport.Generations.ShouldBeEmpty();
}

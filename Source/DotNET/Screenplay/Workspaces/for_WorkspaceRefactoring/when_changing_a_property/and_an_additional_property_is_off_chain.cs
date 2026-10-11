// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class and_an_additional_property_is_off_chain : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request() with { AlsoRename = [Select("Unrelated")] });

    [Fact] void should_refuse_the_unrelated_hop() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.HopNotOnChain);
    [Fact] void should_name_its_original_occurrence() => Result.Conflicts.Single().Candidates.Single().Location.Path.ShouldEqual("model.play");
    [Fact] void should_keep_the_whole_request_atomic() => Result.WritePlan.ShouldBeNull();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class and_removal_has_a_reader : given.a_property_chain_workspace
{
    void Because() => Result = Workspace.ProposeProperty(Request("Registered", "remove") with { EventNeverPersisted = true });

    [Fact] void should_refuse() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.PropertyReadersExist);
    [Fact] void should_list_the_automap_reader() => Result.PropertyReport.NeedsDecision.Single().Candidates.Single().Role.ShouldEqual("automap");
    [Fact] void should_not_weaken_any_assertion() => Result.WritePlan.ShouldBeNull();
}

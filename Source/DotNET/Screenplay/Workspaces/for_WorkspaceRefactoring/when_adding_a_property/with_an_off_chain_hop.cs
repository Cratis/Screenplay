// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_an_off_chain_hop : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { Optional = true, Hops = [AddHop("Unrelated")] });

    [Fact] void should_refuse_the_unrelated_declaration() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.HopNotOnChain);
    [Fact] void should_list_the_unrelated_owner() => Result.Conflicts.Single().Candidates.Single().Address.Parts.Any(part => part.Key == "Unrelated").ShouldBeTrue();
}

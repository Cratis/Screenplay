// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_an_existing_property_name : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { Name = "name", Optional = true });

    [Fact] void should_not_merge_with_the_existing_property() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.InvalidOperation);
    [Fact] void should_not_create_a_candidate() => Result.Workspace.ShouldBeNull();
}

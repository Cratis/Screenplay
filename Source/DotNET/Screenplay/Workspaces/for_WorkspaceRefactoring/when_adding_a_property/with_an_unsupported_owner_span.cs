// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_an_unsupported_owner_span : given.a_property_add_workspace
{
    void Because() => Result = Workspace.ProposeProperty(AddRequest() with { Owner = Property("Register").Handle, ExpectedOwnerName = "name" });

    [Fact] void should_require_a_declaration_handle() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedPropertySpan);
    [Fact] void should_not_publish_a_candidate() => Result.Workspace.ShouldBeNull();
}

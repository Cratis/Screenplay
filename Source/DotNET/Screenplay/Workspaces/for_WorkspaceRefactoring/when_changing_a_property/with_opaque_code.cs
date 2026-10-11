// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_opaque_code : given.a_property_chain_workspace
{
    void Establish() => Workspace = Create("""
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                name String
                handler
                  ```csharp
                  return name;
                  ```
        """);

    void Because() => Result = Workspace.ProposeProperty(Request());

    [Fact] void should_refuse_unprovable_code() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.OpaquePropertyReference);
    [Fact] void should_retain_the_code_location() => Result.Conflicts.Single().Candidates.ShouldNotBeEmpty();
}

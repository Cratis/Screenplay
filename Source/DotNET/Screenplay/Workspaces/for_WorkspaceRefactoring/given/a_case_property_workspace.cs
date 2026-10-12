// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_case_property_workspace : a_property_add_workspace
{
    void Establish() => Workspace = Create("""
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                id String identifier
                name String
                produces Registered
                  for id
                  name = name
              event Registered
                name String
              example Request : Register
                id = "project-1"
                name = "Ada"
              example Expected : Registered
                name = "Ada"
              specification Registering
                parameter input String
                case Ready input = "Ada"
                when Request
                  name = case.input
                then Expected
                  name = case.input
        """);

    protected WorkspacePropertyRequest CaseRequest() => AddRequest() with { EventNeverPersisted = true, Hops = [AddHop("Registered"), MappingHop()], Validation = WorkspaceAuthoringValidation.Executable };
}

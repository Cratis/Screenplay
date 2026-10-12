// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_case_supplies_a_fixture_value : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink[] _fixtures;

    void Establish() => _links = Create("""
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                name String
              event Registered
                name String
              specification registering
                parameter input String
                case Ready input = "Ready"
                when Register
                  name = case.input
                then Registered
                  name = case.input
        """);
    void Because() => _fixtures = [.. _links.Incoming(_links.Find("Projects.Registration.Register.Register.name").Single())];

    [Fact] void should_retain_the_case_assignment_location() => _fixtures.Any(link => link.Reference.Name == "input" && link.Reference.Role == "fixture").ShouldBeTrue();
    [Fact] void should_retain_the_step_assignment() => _fixtures.Any(link => link.Reference.Name == "name").ShouldBeTrue();
}

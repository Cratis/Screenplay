// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_distinguishing_fixtures_from_assertions : given.a_model
{
    WorkspacePropertyLinks _links;
    string[] _roles;

    void Establish() => _links = Create("""
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                name String
                produces Registered
                  name = name
              event Registered
                name String
              specification registering
                given Registered
                  name = "Before"
                when Register
                  name = "After"
                then Registered
                  name = "After"
        """);
    void Because() => _roles = [.. _links.Incoming(_links.Find("Projects.Registration.Register.Registered.name").Single()).Select(link => link.Reference.Role)];

    [Fact] void should_classify_the_given_value_as_a_fixture() => _roles.ShouldContain("fixture");
    [Fact] void should_classify_the_then_value_as_an_assertion() => _roles.ShouldContain("assertion");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_linking_a_typed_example_and_query_result : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _example;
    WorkspacePropertyLink _assertion;

    void Establish() => _links = Create("""
        example Ready : Register
          name = "Ready"
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                name String
            slice StateView Lookup
              readmodel Project
                name String
              query Projects => Project[]
              specification looking_up
                when query Projects
                then result
                  name = "Ready"
        """);
    void Because()
    {
        _example = _links.Incoming(_links.Find("Projects.Registration.Register.Register.name").Single()).Single();
        _assertion = _links.Incoming(_links.Find("Projects.Registration.Lookup.Project.name").Single()).Single();
    }

    [Fact] void should_link_the_example_to_its_declared_type() => _example.Reference.Role.ShouldEqual("fixture");
    [Fact] void should_link_the_query_result_to_its_read_model() => _assertion.Reference.Role.ShouldEqual("assertion");
}

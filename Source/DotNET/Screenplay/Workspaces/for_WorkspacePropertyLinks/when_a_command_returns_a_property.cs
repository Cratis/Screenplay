// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_command_returns_a_property : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _response;

    void Establish() => _links = Create("""
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                name String
                returns name
        """);
    void Because() => _response = _links.Incoming(_links.Find("Projects.Registration.Register.Register.name").Single()).Single();

    [Fact] void should_classify_the_response_reader() => _response.Reference.Role.ShouldEqual("response");
    [Fact] void should_resolve_the_command_property() => _response.Resolution.ShouldEqual("resolved");
}

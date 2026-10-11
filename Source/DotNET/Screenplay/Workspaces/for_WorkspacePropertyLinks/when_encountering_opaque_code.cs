// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_encountering_opaque_code : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
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
    void Because() => _link = _links.Incoming(_links.Find("Projects.Registration.Register.Register.name").Single()).Single();

    [Fact] void should_report_the_opaque_role() => _link.Reference.Role.ShouldEqual("opaque");
    [Fact] void should_not_claim_code_resolution() => _link.Resolution.ShouldEqual("incomplete");
    [Fact] void should_retain_its_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
}

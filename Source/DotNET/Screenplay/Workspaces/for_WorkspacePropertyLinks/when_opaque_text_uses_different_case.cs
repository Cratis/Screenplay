// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_opaque_text_uses_different_case : given.a_model
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
                  return item.Name + "NAME"; // name
                  ```
        """);
    void Because() => _link = _links.Incoming(_links.Find("Projects.Registration.Register.Register.name").Single()).Single();

    [Fact] void should_classify_the_whole_word_reader_as_opaque() => _link.Reference.Role.ShouldEqual("opaque");
    [Fact] void should_keep_the_opaque_reader_incomplete() => _link.Resolution.ShouldEqual("incomplete");
}

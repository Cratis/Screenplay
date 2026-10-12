// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_an_opaque_policy_is_inherited : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        policy Owns
          ```csharp
          return ownerId == caller.Id;
          ```
        policy Staff
          require authenticated
        module M
          authorize Owns and Staff
          feature F
            feature Nested
              slice StateChange S
                command Update
                  ownerId String
                command Unrelated
                  name String
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.Nested.S.Update.ownerId").Single()).Single();

    [Fact] void should_record_the_opaque_reader() => _link.Reference.Role.ShouldEqual("opaque");
    [Fact] void should_not_claim_code_resolution() => _link.Resolution.ShouldEqual("incomplete");
    [Fact] void should_retain_the_policy_owner() => _link.Reference.Owner.Address.ShouldEqual("Owns");
    [Fact] void should_retain_the_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
}

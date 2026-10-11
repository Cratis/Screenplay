// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_declarative_policy_is_inherited : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        policy Owns
          require claim "owner" matches ownerId
        policy Staff
          require authenticated
        module M
          authorize Staff
          feature F
            authorize Owns or Staff
            feature Nested
              slice StateChange S
                command Update
                  ownerId String
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.Nested.S.Update.ownerId").Single()).Single();

    [Fact] void should_record_the_condition_reader() => _link.Reference.Role.ShouldEqual("condition");
    [Fact] void should_resolve_the_inherited_command_member() => _link.Resolution.ShouldEqual("resolved");
    [Fact] void should_retain_the_policy_owner() => _link.Reference.Owner.Address.ShouldEqual("Owns");
    [Fact] void should_retain_the_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
}

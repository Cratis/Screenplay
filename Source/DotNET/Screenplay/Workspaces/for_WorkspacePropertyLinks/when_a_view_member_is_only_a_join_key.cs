// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_view_member_is_only_a_join_key : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        module M
          feature F
            slice StateView S
              event Changed
                name String
              readmodel Account
                accountId Uuid
              projection Accounts => Account
                no automap
                join other on accountId
                  with Changed
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.S.Account.accountId").Single()).Single();

    [Fact] void should_record_the_key_reader() => _link.Reference.Role.ShouldEqual("key");
    [Fact] void should_resolve_the_view_member() => _link.Resolution.ShouldEqual("resolved");
    [Fact] void should_retain_the_owner() => _link.Reference.Owner.Address.ShouldEqual("M.F.S.Accounts");
    [Fact] void should_retain_the_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
}

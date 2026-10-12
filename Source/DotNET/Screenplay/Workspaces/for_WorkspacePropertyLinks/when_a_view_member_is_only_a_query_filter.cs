// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_view_member_is_only_a_query_filter : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        module M
          feature F
            slice StateView S
              readmodel Account
                status String
              query Accounts => Account[]
                filter status String
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.S.Account.status").Single()).Single();

    [Fact] void should_record_the_condition_reader() => _link.Reference.Role.ShouldEqual("condition");
    [Fact] void should_resolve_the_view_member() => _link.Resolution.ShouldEqual("resolved");
    [Fact] void should_retain_the_owner() => _link.Reference.Owner.Address.ShouldEqual("M.F.S.Accounts");
    [Fact] void should_retain_the_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
}

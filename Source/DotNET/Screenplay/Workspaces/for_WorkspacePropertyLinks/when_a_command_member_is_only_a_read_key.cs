// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_command_member_is_only_a_read_key : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        module M
          feature F
            slice StateChange S
              readmodel Account
                value Int
              command Transfer
                accountId Uuid
                reads Account by accountId
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.S.Transfer.accountId").Single()).Single();

    [Fact] void should_record_the_key_reader() => _link.Reference.Role.ShouldEqual("key");
    [Fact] void should_mark_legacy_reads_incomplete() => _link.Resolution.ShouldEqual("incomplete");
    [Fact] void should_retain_the_owner() => _link.Reference.Owner.Address.ShouldEqual("M.F.S.Transfer");
    [Fact] void should_retain_the_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
}

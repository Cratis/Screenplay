// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_mapping_reads_through_an_alias : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        module M
          feature F
            slice StateView V
              readmodel Account
                balance Int
            slice StateChange S
              command Transfer
                reads Account as account
                produces Transferred
                  amount = account.balance
              event Transferred
                amount Int
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.V.Account.balance").Single()).Single();

    [Fact] void should_keep_the_mapping_source() => _link.Reference.Role.ShouldEqual("mapping-source");
    [Fact] void should_keep_the_counterpart() => _link.OtherEnd.Address.ShouldEqual("M.F.S.Transferred.amount");
    [Fact] void should_mark_legacy_reads_incomplete() => _link.Resolution.ShouldEqual("incomplete");
    [Fact] void should_mark_the_counterpart_incomplete() => _links.Complete(_links.Find("M.F.S.Transferred.amount").Single()).ShouldBeFalse();
    [Fact] void should_retain_the_owner() => _link.Reference.Owner.Address.ShouldEqual("M.F.S.Transfer");
    [Fact] void should_retain_the_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
}

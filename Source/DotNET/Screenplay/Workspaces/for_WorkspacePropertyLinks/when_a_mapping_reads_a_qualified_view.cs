// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_mapping_reads_a_qualified_view : given.a_model
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
                reads Account
                produces Transferred
                  amount = M.F.V.Account.balance
              event Transferred
                amount Int
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.V.Account.balance").Single()).Single();

    [Fact] void should_keep_the_qualified_mapping() => _link.Reference.Name.ShouldEqual("M.F.V.Account.balance");
    [Fact] void should_not_claim_legacy_execution() => _link.Resolution.ShouldEqual("incomplete");
    [Fact] void should_keep_the_counterpart() => _link.OtherEnd.Address.ShouldEqual("M.F.S.Transferred.amount");
}

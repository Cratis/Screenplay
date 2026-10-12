// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_qualified_read_has_a_nested_member_path : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        type Details
          balance Int
        module M
          feature F
            slice StateView V
              readmodel Account
                details Details
            slice StateChange S
              command Transfer
                reads Account
                produces Transferred
                  amount = M.F.V.Account.details.balance
              event Transferred
                amount Int
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.V.Account.details").Single()).Single();

    [Fact] void should_retain_the_root_member_candidate() => _link.Candidates.Single().Address.ShouldEqual("M.F.V.Account.details");
    [Fact] void should_not_claim_nested_resolution() => _link.Resolution.ShouldEqual("incomplete");
    [Fact] void should_mark_the_property_incomplete() => _links.Complete(_links.Find("M.F.V.Account.details").Single()).ShouldBeFalse();
}

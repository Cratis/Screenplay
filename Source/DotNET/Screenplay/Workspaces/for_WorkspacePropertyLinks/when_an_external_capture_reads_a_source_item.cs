// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_an_external_capture_reads_a_source_item : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        module M
          feature F
            slice Translate S
              event Received
                id Uuid
              capture Arrival
                source webhook
                  route /arrivals
                key id
                append Received
                  id = $.id
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.S.Received.id").Single()).Single();

    [Fact] void should_mark_the_target_incomplete() => _link.Resolution.ShouldEqual("incomplete");
    [Fact] void should_not_guess_a_source_property() => _link.OtherEnd.ShouldBeNull();
    [Fact] void should_retain_the_unresolved_source() => _links.Links.Single(link => link.Reference.Role == "mapping-source").Resolution.ShouldEqual("unresolved");
    [Fact] void should_retain_the_capture_owner() => _link.Reference.Owner.Address.ShouldEqual("M.F.S.Arrival");
}

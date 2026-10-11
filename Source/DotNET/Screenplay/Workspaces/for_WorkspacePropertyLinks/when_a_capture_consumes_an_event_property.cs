// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_capture_consumes_an_event_property : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        module M
          feature F
            slice Translate S
              direction inbound
              event Arrived generation 1 from "other/store"
                id Uuid
              event Arrived generation 2 from "other/store"
                id Uuid
              event Received
                id Uuid
              capture Arrival
                source events
                  from Arrived
                key id
                append Received
                  id = $.id
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.S.Arrived.id").Single()).Single();

    [Fact] void should_record_the_source_item_reader() => _link.Reference.Name.ShouldEqual("$.id");
    [Fact] void should_resolve_the_current_event_member() => _link.Resolution.ShouldEqual("resolved");
    [Fact] void should_connect_the_appended_member() => _link.OtherEnd.Address.ShouldEqual("M.F.S.Received.id");
    [Fact] void should_connect_the_reverse_mapping() => _links.Incoming(_links.Find("M.F.S.Received.id").Single()).Single().OtherEnd.Address.ShouldEqual("M.F.S.Arrived.id");
    [Fact] void should_not_link_the_historical_member() => _links.Incoming(_links.Find("M.F.S.Arrived.generation1.id").Single()).ShouldBeEmpty();
    [Fact] void should_retain_the_capture_owner() => _link.Reference.Owner.Address.ShouldEqual("M.F.S.Arrival");
    [Fact] void should_retain_the_location() => _link.Reference.Location.Path.ShouldEqual("model.play");
}

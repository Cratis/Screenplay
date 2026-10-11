// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_a_capture_has_multiple_event_sources : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish() => _links = Create("""
        module M
          feature F
            slice Translate S
              direction inbound
              event Arrived from "other/store"
                id Uuid
              event Imported from "other/store"
                id Uuid
              event Received
                id Uuid
              capture Arrival
                source events
                  from Arrived
                  from Imported
                key id
                append Received
                  id = $.id
        """);
    void Because() => _link = _links.Incoming(_links.Find("M.F.S.Arrived.id").Single()).Single();

    [Fact] void should_keep_both_candidates() => _link.Candidates.Select(candidate => candidate.Address).ShouldEqual("M.F.S.Arrived.id", "M.F.S.Imported.id");
    [Fact] void should_report_ambiguity() => _link.Resolution.ShouldEqual("ambiguous");
    [Fact] void should_mark_the_target_incomplete() => _links.Complete(_links.Find("M.F.S.Received.id").Single()).ShouldBeFalse();
}

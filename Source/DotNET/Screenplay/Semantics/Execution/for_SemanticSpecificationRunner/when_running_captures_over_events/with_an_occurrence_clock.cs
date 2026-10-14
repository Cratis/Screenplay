// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_an_occurrence_clock : given.an_event_capture
{
    void Because() => RunCapture("""
        append Stamped
          occurred = $context.occurred
        """, """
        given clock "2026-10-02T12:00:00Z"
        when append Arrived
          id = "parcel"
          status = "sent"
        then Stamped
          for "parcel"
          occurred = "2026-10-02T12:00:00Z"
        """);

    [Fact] void should_map_the_incoming_occurrence() => _run.Passed.ShouldBeTrue();
    [Fact] void should_occur_at_the_same_instant() => ((SemanticAccepted)_run.Execution).Facts[1].Occurred.ShouldEqual(((SemanticAccepted)_run.Execution).Facts[0].Occurred);
}

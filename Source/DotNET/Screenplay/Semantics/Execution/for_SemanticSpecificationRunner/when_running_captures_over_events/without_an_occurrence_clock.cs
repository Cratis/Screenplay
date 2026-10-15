// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class without_an_occurrence_clock : given.an_event_capture
{
    void Because() => RunCapture("""
        append Stamped
          occurred = $context.occurred
        """, """
        when append Arrived
          id = "parcel"
          status = "sent"
        then Stamped
          occurred = "2026-10-02T12:00:00Z"
        """);

    [Fact] void should_reject_unknown_occurrence_instead_of_inventing_a_time() => _run.Execution.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_append_no_capture_fact() => _run.Execution.World.Facts.Length.ShouldEqual(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_an_identical_event : given.an_event_capture
{
    void Because() => RunCapture("""
        append Translated
          id = $.id
          status = $.status
        append Added
          when added
        append Modified
          when status
        """, """
        given Arrived
          id = "parcel"
          status = "sent"
        when append Arrived
          id = "parcel"
          status = "sent"
        then no events
        """);

    [Fact] void should_append_nothing_for_unchanged_state() => _run.Passed.ShouldBeTrue();
}

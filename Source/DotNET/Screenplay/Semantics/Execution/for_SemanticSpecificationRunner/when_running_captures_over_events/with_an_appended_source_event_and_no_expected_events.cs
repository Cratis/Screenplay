// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_an_appended_source_event_and_no_expected_events : given.an_event_capture
{
    void Because() => RunCapture("""
        append Translated
          id = $.id
          status = $.status
        """, """
        when append Arrived
          id = "parcel"
          status = "sent"
        then no events
        """);

    [Fact] void should_fail_because_the_capture_appends() => _run.Passed.ShouldBeFalse();
}

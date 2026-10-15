// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_an_appended_source_event : given.an_event_capture
{
    void Because() => RunCapture("""
        append Translated
          id = $.id
          status = $.status
        """, """
        when append Arrived
          id = "parcel"
          status = "sent"
        then Translated
          for "parcel"
          id = "parcel"
          status = "sent"
        """);

    [Fact] void should_map_the_event_and_append_to_its_key() => _run.Passed.ShouldBeTrue();
    [Fact] void should_not_persist_the_capture_state() => _run.Execution.World.Facts.Length.ShouldEqual(2);
}

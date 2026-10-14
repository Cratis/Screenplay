// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_two_source_events : given.an_event_capture
{
    void Because() => RunCapture("""
        append Translated
          when label
          id = $.id
          status = $.status
          label = $.label
        """, """
        given Arrived
          id = "parcel"
          status = "sent"
        given Arrived
          id = "other"
          status = "delivered"
        when append Labelled
          id = "parcel"
          label = "priority"
        then Translated for "parcel"
          id = "parcel"
          status = "sent"
          label = "priority"
        """);

    [Fact] void should_overlay_properties_and_keep_state_separate_per_key() => _run.Passed.ShouldBeTrue();
}

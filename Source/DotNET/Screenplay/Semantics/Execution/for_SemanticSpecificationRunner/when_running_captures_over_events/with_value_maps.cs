// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_value_maps : given.an_event_capture
{
    void Because() => RunCapture("""
        map
          status = status translate
            "sendt" => sent
            "levert" => delivered
        append Translated
          when status from "sent" to "delivered"
          id = $.id
          status = $.status
        """, """
        given Arrived
          id = "parcel"
          status = "sendt"
        when append Arrived
          id = "parcel"
          status = "levert"
        then Translated
          id = "parcel"
          status = "delivered"
        """);

    [Fact] void should_compare_and_read_mapped_states() => _run.Passed.ShouldBeTrue();
}

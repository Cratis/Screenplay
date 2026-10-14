// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_history_before_a_transition : given.an_event_capture
{
    void Because() => RunCapture("""
        append Translated
          when status from "sent" to "delivered"
          id = $.id
          status = $.status
        """, """
        given Arrived for "foreign-stream"
          id = "parcel"
          status = "new"
        given Arrived
          id = "parcel"
          status = "sent"
        when append Arrived
          id = "parcel"
          status = "delivered"
        then Translated
          id = "parcel"
          status = "delivered"
        """);

    [Fact] void should_fold_givens_in_order_before_the_action() => _run.Passed.ShouldBeTrue();
    [Fact] void should_not_append_while_folding_history() => ((SemanticAccepted)_run.Execution).Facts.Length.ShouldEqual(2);
}

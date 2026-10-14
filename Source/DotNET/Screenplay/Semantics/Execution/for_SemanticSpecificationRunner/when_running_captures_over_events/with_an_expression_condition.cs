// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_an_expression_condition : given.an_event_capture
{
    void Because() => RunCapture("""
        append Added
          when `status == "sent"`
        """, """
        when append Arrived
          id = "parcel"
          status = "sent"
        then Added
        """);

    [Fact] void should_evaluate_the_expression_on_the_added_after_state() => _run.Passed.ShouldBeTrue();
}

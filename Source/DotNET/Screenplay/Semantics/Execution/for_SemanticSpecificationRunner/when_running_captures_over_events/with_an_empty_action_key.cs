// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_an_empty_action_key : given.an_event_capture
{
    void Because() => RunCapture("append Added", """
        when append Arrived
          id = "   "
          status = "sent"
        then Added
        """);

    [Fact] void should_fail_with_a_typed_contract_rejection() => _run.Execution.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_name_the_capture_event_and_key() => ((SemanticRejected)_run.Execution).Details.ShouldContain("Capture 'Feed' received event 'Arrived' with an empty key 'id'");
    [Fact] void should_append_no_capture_event() => _run.Execution.World.Facts.Length.ShouldEqual(1);
}

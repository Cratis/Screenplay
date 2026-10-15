// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_an_empty_history_key : given.an_event_capture
{
    void Because() => RunCapture("append Added", """
        given Arrived
          id = ""
          status = "sent"
        when append Arrived
          id = "parcel"
          status = "delivered"
        then Added
        """);

    [Fact] void should_fail_before_the_action() => _run.Execution.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_leave_only_the_given_fact() => _run.Execution.World.Facts.Length.ShouldEqual(1);
    [Fact] void should_name_the_empty_history_key() => ((SemanticRejected)_run.Execution).Details.ShouldContain("empty key 'id'");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_the_first_event_for_a_key : given.an_event_capture
{
    void Because() => RunCapture("""
        append Added
          when added
        append Modified
          when status
        append Modified
          when id and status
        append Removed
          when removed
        """, """
        when append Arrived
          id = "parcel"
          status = "sent"
        then Added
        """);

    [Fact] void should_be_added_not_modified_or_removed() => _run.Passed.ShouldBeTrue();
    [Fact] void should_append_only_one_capture_event() => ((SemanticAccepted)_run.Execution).Facts.Length.ShouldEqual(2);
}

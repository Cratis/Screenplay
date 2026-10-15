// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_children : given.an_event_capture
{
    void Because() => RunCapture("""
        append Added
        children items identified by id
          append Modified
            when added
        """, """
        when append Arrived
          id = "parcel"
          status = "sent"
        then Added
        """);

    [Fact] void should_fail_closed_before_any_capture_append() => _run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_not_append_the_root_effect() => _run.Execution.World.Facts.Length.ShouldEqual(1);
}

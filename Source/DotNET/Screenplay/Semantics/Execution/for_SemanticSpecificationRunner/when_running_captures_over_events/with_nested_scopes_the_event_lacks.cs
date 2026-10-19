// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_nested_scopes_the_event_lacks : given.an_event_capture
{
    void Because() => RunCapture("""
        append Added
        nested contact
          append Modified
            when added
        """, """
        when append Arrived
          id = "parcel"
          status = "sent"
        then Added
        """);

    // The event has no 'contact', so there is nothing to scope into: the root effect runs and no scoped append does.
    [Fact] void should_run_the_scenario() => _run.Execution.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_append_the_root_effect_and_no_scoped_one() => _run.Execution.World.Facts.Length.ShouldEqual(2);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_a_structured_watched_field : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    SemanticSpecificationRun _run;

    void Because()
    {
        Compile("""
            module M
              feature F
                slice Translate Receive
                  direction inbound
                  event Arrived from "store"
                    id String
                    labels String[]
                  event Local
                  capture Feed
                    source events
                      from Arrived
                    key id
                    append Local
                      when labels
                  specification S
                    when append Arrived
                      id = "parcel"
                      labels = ["priority"]
                    then Local
            """);
        _run = Run("S");
    }

    [Fact] void should_refuse_a_structured_guard_even_on_first_arrival() => _run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_append_no_capture_event() => _run.Execution.World.Facts.Length.ShouldEqual(1);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_an_event_source_key : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    SemanticSpecificationRun _agreeing;
    SemanticSpecificationRun _disagreeing;

    void Because()
    {
        _agreeing = RunWithSource("parcel");
        _disagreeing = RunWithSource("other");
    }

    [Fact] void should_append_to_the_key_when_the_source_agrees() => _agreeing.Passed.ShouldBeTrue();
    [Fact] void should_reject_a_source_that_differs_from_the_key() => _disagreeing.Execution.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_name_the_disagreement() => ((SemanticRejected)_disagreeing.Execution).Details.ShouldContain("keys by event source");

    SemanticSpecificationRun RunWithSource(string source)
    {
        Compile($$"""
            module Shipping
              feature Tracking
                slice Translate Receive
                  direction inbound
                  event Arrived from "carrier"
                    id String
                  event Translated
                    id String
                  capture Feed
                    source events
                      from Arrived
                    key id is event source
                    append Translated
                      id = $.id
                  specification S
                    given Arrived
                      for "{{source}}"
                      id = "parcel"
                    then Translated
                      for "parcel"
                      id = "parcel"
            """);

        return Run("S");
    }
}

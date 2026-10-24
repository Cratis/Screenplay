// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class with_conflicting_event_source_keys : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    SemanticSpecificationRun _explicit;
    SemanticSpecificationRun _inferred;

    void Because()
    {
        _explicit = RunWithSource("for \"parcel\"");
        _inferred = RunWithSource(string.Empty);
    }

    [Fact] void should_validate_every_capture_against_an_explicit_source() => _explicit.Execution.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_validate_every_capture_against_an_inferred_source() => _inferred.Execution.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_name_the_explicit_disagreement() => ((SemanticRejected)_explicit.Execution).Details.ShouldContain("keys by event source");
    [Fact] void should_name_the_inferred_disagreement() => ((SemanticRejected)_inferred.Execution).Details.ShouldContain("keys by event source");

    SemanticSpecificationRun RunWithSource(string source)
    {
        Compile($$"""
            module Shipping
              feature Tracking
                slice Translate Receive
                  direction inbound
                  event Arrived from "carrier"
                    id String
                    other String
                  capture First
                    source events
                      from Arrived
                    key id is event source
                  capture Second
                    source events
                      from Arrived
                    key other is event source
                  readmodel Seen
                    id String
                  query SeenById => Seen?
                    by id String
                  projection SeenProjection => Seen
                    from Arrived
                      id = id
                  specification S
                    given Arrived
                      {{source}}
                      id = "parcel"
                      other = "different"
                    then readmodel Seen
                      id = "parcel"
            """);

        return Run("S");
    }
}

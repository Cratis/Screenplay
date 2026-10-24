// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

public class without_a_source_on_a_marked_fixture : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    SemanticSpecificationRun _history;
    SemanticSpecificationRun _action;
    SemanticSpecificationRun _mismatched;

    void Because()
    {
        _history = RunSpecification("""
            given Arrived
              id = "parcel"
            then readmodel Seen
              source = "parcel"
              id = "parcel"
            """);
        _action = RunSpecification("""
            when append Arrived
              id = "parcel"
            then readmodel Seen
              source = "parcel"
              id = "parcel"
            """);
        _mismatched = RunSpecification("""
            given Arrived
              for "other"
              id = "parcel"
            then readmodel Seen
              source = "other"
              id = "parcel"
            """);
    }

    [Fact] void should_take_the_key_as_the_source_of_a_history_fixture() => _history.Passed.ShouldBeTrue();
    [Fact] void should_take_the_key_as_the_source_of_an_action_fixture() => _action.Passed.ShouldBeTrue();
    [Fact] void should_reject_a_mismatched_source_without_an_action() => _mismatched.Execution.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_name_the_disagreement() => ((SemanticRejected)_mismatched.Execution).Details.ShouldContain("keys by event source");

    SemanticSpecificationRun RunSpecification(string steps)
    {
        Compile($$"""
            module Shipping
              feature Tracking
                slice Translate Receive
                  direction inbound
                  event Arrived from "carrier"
                    id String
                  capture Feed
                    source events
                      from Arrived
                    key id is event source
                  readmodel Seen
                    source String
                    id String
                  query SeenBySource => Seen?
                    by source String
                  projection SeenProjection => Seen
                    from Arrived
                      source = $eventSourceId
                      id = id
                  specification S
            {{string.Join('\n', steps.Split('\n').Select(line => "        " + line))}}
            """);

        return Run("S");
    }
}

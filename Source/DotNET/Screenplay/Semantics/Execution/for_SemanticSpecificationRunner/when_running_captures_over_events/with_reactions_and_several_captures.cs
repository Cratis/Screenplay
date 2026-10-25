// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_captures_over_events;

// The specification runs only its own slice: the follow-up automation's reactions are another slice's and do not run.
public class with_reactions_and_several_captures : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    SemanticSpecificationRun _run;

    void Because()
    {
        Compile("""
            module Shipping
              feature Tracking
                slice Translate Receive
                  direction inbound
                  event Arrived from "carrier"
                    id String
                  event SourceReacted
                  event First
                    name String
                  event Second
                    name String
                  reaction Inbound
                    when Arrived
                      produces SourceReacted
                        for id
                  capture Alpha
                    source events
                      from Arrived
                    key id
                    append First
                      name = "Alpha"
                    append Second
                      name = "Alpha"
                  capture Beta
                    source events
                      from Arrived
                    key id
                    append First
                      name = "Beta"
                    append Second
                      name = "Beta"
                  specification S
                    when append Arrived
                      id = "parcel"
                    then events in any order
                    then SourceReacted
                    then First
                      name = "Alpha"
                    then Second
                      name = "Alpha"
                    then First
                      name = "Beta"
                    then Second
                      name = "Beta"
                slice Automation FollowUp
                  event SourceSettled
                  event Cascade
                    name String
                  reaction SettleSource
                    when SourceReacted
                      produces SourceSettled
                  reaction SettleCapture
                    when First
                      produces Cascade
                        name = name
            """);
        _run = Run("S");
    }

    [Fact] void should_settle_its_own_appends() => Assert.True(_run.Passed, string.Join('\n', _run.Failures));
    [Fact] void should_settle_the_source_reaction_before_capture_delivery() => ((SemanticAccepted)_run.Execution).Facts.Take(2).Select(fact => _plan.Events[fact.EventContract].Name).ShouldEqual(new[] { "Arrived", "SourceReacted" });
    [Fact] void should_deliver_in_semantic_identity_order() => ((SemanticAccepted)_run.Execution).Facts.Skip(2).Where(fact => _plan.Events[fact.EventContract].Name == "First").Select(fact => ((SemanticTextValue)fact.Values.Single().Value).Value).ShouldEqual(_plan.Captures.Values.OrderBy(capture => capture.Id.ToString(), StringComparer.Ordinal).Select(capture => capture.Name));
    [Fact] void should_settle_each_capture_append_before_the_next() => ((SemanticAccepted)_run.Execution).Facts.Skip(2).Select(fact => _plan.Events[fact.EventContract].Name).ShouldEqual(new[] { "First", "Second", "First", "Second" });
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

// The invoked command is the automation's own, so its specification runs it in full.
public class and_the_invocation_uses_the_event_source : given.a_v6_scenario
{
    [Theory]
    [InlineData("$eventSourceId")]
    [InlineData("$eventContext.eventSourceId")]
    void should_append_on_the_triggering_events_source_without_a_clock_or_caller(string source)
    {
        Compile(
            $$"""
            concept OrderId : String
            module Orders
              feature Fulfilment
                slice StateChange PlaceOrder
                  command PlaceOrder
                    orderId OrderId identifier
                    produces OrderPlaced
                      for orderId
                  event OrderPlaced
                slice Automation Picking
                  command StartPicking
                    orderId OrderId identifier
                    produces PickingStarted
                      for orderId
                  event PickingStarted
                  reaction PickOnPlacement
                    when OrderPlaced
                      invokes StartPicking
                        orderId = {{source}}
                  specification StartingPicking
                    when append OrderPlaced
                      for "order-42"
                    then PickingStarted
                      for "order-42"
            """);
        var run = Run("StartingPicking");
        run.Passed.ShouldBeTrue();
        var accepted = (SemanticAccepted)run.Execution;
        accepted.Facts.Length.ShouldEqual(2);
        accepted.Facts.Select(fact => fact.Destination).ShouldEqual([SemanticValue.Text("order-42"), SemanticValue.Text("order-42")]);
    }
}

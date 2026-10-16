// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_a_production_uses_the_event_source : given.a_v6_scenario
{
    [Theory]
    [InlineData("")]
    [InlineData("reaction batches atomic")]
    void should_map_the_source_without_requiring_a_clock_or_audit_identity(string mode)
    {
        Compile(
            $$"""
            {{mode}}
            module Orders
              feature Fulfilment
                slice Automation Picking
                  command PlaceOrder
                    orderId String identifier
                    produces OrderPlaced
                      for orderId
                  event OrderPlaced
                  event PickingRequested
                    source String
                  reaction PickOnPlacement
                    when OrderPlaced
                      produces PickingRequested
                        source = $eventSourceId
                  specification RequestingPicking
                    when append OrderPlaced
                      for "order-42"
                    then PickingRequested
                      source = "order-42"
            """);
        var run = Run("RequestingPicking");
        run.Passed.ShouldBeTrue();
        ((SemanticAccepted)run.Execution).Facts.Last().Destination.ShouldEqual(SemanticValue.Text("order-42"));
    }
}

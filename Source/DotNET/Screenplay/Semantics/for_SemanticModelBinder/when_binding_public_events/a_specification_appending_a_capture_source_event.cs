// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_public_events;

public class a_specification_appending_a_capture_source_event : given.a_public_events_model
{
    SemanticSpecificationRun _run;

    void Because()
    {
        var result = Bind("""
            concept OrderId : Uuid
            module Shipping
              feature Tracking
                slice Translate TrackShipments
                  direction inbound
                  event ShipmentDispatched from "shipping"
                    orderId OrderId
                  event OrderDispatched
                    orderId OrderId
                  capture ShipmentTracking
                    source events
                      from ShipmentDispatched
                    key orderId
                    append OrderDispatched
                      orderId = $.orderId
                  specification TrackingADispatch
                    when append ShipmentDispatched
                      for "carrier-stream"
                      orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    then OrderDispatched
                      for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                      orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            """);
        Assert.True(result.Success, Messages(result));
        var plan = SemanticExecutionPlan.Compile(result.Value!.Model).Plan!;
        _run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
    }

    [Fact] void should_execute_the_typed_event_fixture() => _run.Passed.ShouldBeTrue();
    [Fact] void should_keep_the_foreign_source_opaque() => ((SemanticAccepted)_run.Execution).Facts[0].Context!.EventSource.Value.ShouldEqual(SemanticValue.Text("carrier-stream"));
    [Fact] void should_append_to_the_capture_key() => ((SemanticAccepted)_run.Execution).Facts[1].Destination.ShouldEqual(SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"));
}

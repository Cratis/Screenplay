// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_public_events;

public class a_specification_driving_a_capture_over_public_events : given.a_public_events_model
{
    const string Source =
        """
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
                when capture ShipmentTracking
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then OrderDispatched
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        """;

    CompilationResult<SemanticCompilation> _result;
    SemanticSpecificationRun _run;

    void Because()
    {
        _result = Bind(Source);
        Assert.True(_result.Success, Messages(_result));
        var plan = SemanticExecutionPlan.Compile(_result.Value!.Model).Plan!;
        _run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
    }

    [Fact] void should_report_an_unsupported_outcome() => _run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_name_the_specification_capability() => ((SemanticUnsupported)_run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Specification);
    [Fact] void should_name_the_capture() => ((SemanticUnsupported)_run.Execution).Details.ShouldContain("ShipmentTracking");
    [Fact] void should_not_count_it_as_passed() => _run.Passed.ShouldBeFalse();
}

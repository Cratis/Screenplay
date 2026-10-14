// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

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
    void Because() => _result = Bind(Source);

    [Fact] void should_refuse_capture_records() => _result.Success.ShouldBeFalse();
    [Fact] void should_require_an_event_fixture() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.CaptureEventsRecordFixture);
}

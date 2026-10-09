// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

public class a_public_events_model : a_semantic_binder
{
    protected const string Order = "3fa85f64-5717-4562-b3fc-2c963f66afa6";

    protected const string Outbound =
        """
        concept OrderId : Uuid
        concept OrderStatus : String
        module Shipping
          feature Orders
            slice StateChange PackOrder
              command PackOrder
                orderId OrderId identifier
                produces OrderPacked
                  for orderId
                  orderId = orderId
              event OrderPacked
                orderId OrderId
            slice Translate PublishOrderShipped
              direction outbound
              public event OrderShipped
                orderId OrderId
                status OrderStatus
              projection OrderShippedPublisher => OrderShipped
                from OrderPacked
                  status = "packed"
              specification PublishingAPackedOrder
                when append OrderPacked
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then OrderShipped
                  orderId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  status = "packed"
        """;

    protected const string Inbound =
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
        """;

    protected SemanticSlice Slice(CompilationResult<SemanticCompilation> result, string name) =>
        result.Value!.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Single(slice => slice.Name == name);

    protected static string Messages(CompilationResult<SemanticCompilation> result) =>
        string.Join('\n', result.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));
}

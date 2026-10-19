// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

// Decision 0098 (#858): a reaction invokes a command whose identifier is generated; the specification pins it.
public class when_pinning_generated_values_for_reaction_invoked_commands : given.a_v6_scenario
{
    const string Source =
        """
        concept ShipmentId : Uuid
        module Shipping
          feature Dispatch
            slice Automation Dispatching
              event OrderConfirmed
                orderId String
              event ShipmentCreated
                shipmentId ShipmentId
                orderId String
              command CreateShipment
                shipmentId ShipmentId generated identifier
                orderId String
                produces ShipmentCreated
                  for shipmentId
                  shipmentId = shipmentId
                  orderId = orderId
              reaction DispatchConfirmedOrders
                when OrderConfirmed
                  orderId
                  invokes CreateShipment
                    orderId = orderId
              specification ConfirmingAnOrderCreatesAShipment
                given generated CreateShipment.shipmentId = "11111111-1111-4111-8111-111111111111"
                when append OrderConfirmed
                  orderId = "o1"
                then ShipmentCreated
                  for "11111111-1111-4111-8111-111111111111"
                  shipmentId = "11111111-1111-4111-8111-111111111111"
                  orderId = "o1"
              specification TwoOrdersTakeTheValuesInOrder
                given generated CreateShipment.shipmentId = "11111111-1111-4111-8111-111111111111"
                given generated CreateShipment.shipmentId = "22222222-2222-4222-8222-222222222222"
                given OrderConfirmed
                  orderId = "o0"
                when append OrderConfirmed
                  orderId = "o1"
                then ShipmentCreated
                  for "22222222-2222-4222-8222-222222222222"
                  shipmentId = "22222222-2222-4222-8222-222222222222"
                  orderId = "o1"
              specification NothingPinned
                when append OrderConfirmed
                  orderId = "o1"
                then ShipmentCreated
                  for "11111111-1111-4111-8111-111111111111"
                  shipmentId = "11111111-1111-4111-8111-111111111111"
                  orderId = "o1"
        """;

    SemanticSpecificationRun _pinned;
    SemanticSpecificationRun _unpinned;

    void Because()
    {
        Compile(Source);
        _pinned = Run("ConfirmingAnOrderCreatesAShipment");
        _unpinned = Run("NothingPinned");
    }

    [Fact] void should_compile_at_esm_v30() => _plan.Model.SemanticVersion.ShouldEqual(SemanticVersion.V30);
    [Fact] void should_take_the_pinned_value() => Assert.True(_pinned.Passed, string.Join('\n', _pinned.Failures));
    [Fact] void should_keep_saying_unsupported_without_a_pinned_value() => _unpinned.Execution.ShouldBeOfExactType<SemanticUnsupported>();
    [Fact] void should_round_trip_the_pinned_values_through_the_canonical_json() => Serialization.SemanticModelSerializer.Deserialize(Serialization.SemanticModelSerializer.Serialize(_plan.Model)).Revision.ShouldEqual(_plan.Model.Revision);
}

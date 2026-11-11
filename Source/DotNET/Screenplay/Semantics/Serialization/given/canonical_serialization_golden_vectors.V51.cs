// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.Serialization.given;

/// <summary>
/// Supplies the ESM v51 golden: every v50 construct plus a reaction that invokes an ability and handles its answer.
/// </summary>
public static partial class canonical_serialization_golden_vectors
{
    /// <summary>
    /// The source of the v51 golden: the v50 golden plus a reaction that invokes <c>Carrier.BookPickup</c>.
    /// </summary>
    public const string AbilityInvocationSource = AbilitySource + """


        module Shipping
          feature Pickup
            slice Automation PickupBooking
              event OrderPacked
                parcelId ParcelId
              event PickupBooked
                trackingNumber TrackingNumber
              event PickupUnavailable
                parcelId ParcelId
              event PickupBookingFailed
                reason String
              readmodel ParcelStatus
                parcelId ParcelId
                lastEvent String
              query ParcelStatusById => ParcelStatus optional
                by parcelId ParcelId
              projection ParcelStatusProjection => ParcelStatus
                from OrderPacked
                  parcelId = $eventSourceId
                  lastEvent = "packed"
                from PickupBooked
                  parcelId = $eventSourceId
                  lastEvent = "booked"
                from PickupUnavailable
                  parcelId = $eventSourceId
                  lastEvent = "unavailable"
                from PickupBookingFailed
                  parcelId = $eventSourceId
                  lastEvent = "failed"
              reaction PickupBooking
                runs as system
                when OrderPacked
                  parcelId
                  invokes Carrier.BookPickup
                    parcelId = parcelId
                    on returned
                      produces PickupBooked
                        trackingNumber = $result.trackingNumber
                    on NoCapacity
                      produces PickupUnavailable
                        parcelId = parcelId
                    on failed
                      produces PickupBookingFailed
                        reason = $failure.reason
              specification TheCarrierReturnsATrackingNumber
                given Carrier.BookPickup returns
                  trackingNumber = "TRK-1"
                when append OrderPacked
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then PickupBooked
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  trackingNumber = "TRK-1"
                then invokes Carrier.BookPickup
                  parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
              specification TheCarrierHasNoCapacity
                given Carrier.BookPickup answers NoCapacity
                when append OrderPacked
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then PickupUnavailable
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
              specification TheCarrierFails
                given Carrier.BookPickup fails "down"
                when append OrderPacked
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then PickupBookingFailed
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  reason = "down"
        """;

    /// <summary>
    /// Gets the canonical bytes of the v51 golden.
    /// </summary>
    public static byte[] EsmV51Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v51.json");

    /// <summary>
    /// Creates the v51 golden model.
    /// </summary>
    /// <returns>The model.</returns>
    public static ExecutableSemanticModel CreateSemanticModelV51()
    {
        var result = property_order_binding.AdmitScreens(AbilityInvocationSource);
        if (!result.Success) throw new InvalidSemanticContract(string.Join('\n', result.Refusals));
        return result.Compilation!.Model;
    }
}
#endif

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

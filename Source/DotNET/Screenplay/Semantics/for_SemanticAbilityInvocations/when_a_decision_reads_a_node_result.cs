// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_a_decision_reads_a_node_result : Specification
{
    const string Source =
        """
        concept ParcelId : Uuid
        concept TrackingNumber : String

        system Carrier
          ability BookPickup idempotent
            parcelId ParcelId
            address String
            returns
              trackingNumber TrackingNumber
            outcome NoCapacity

        system Chat
          ability Post
            channel String
            text String

        module Shipping
          feature Pickup
            slice StateChange PackParcelSlice
              command PackParcel
                parcelId ParcelId identifier
                produces ParcelPacked
                  for parcelId
              event ParcelPacked
              event PickupBooked
            slice StateView ParcelView
              readmodel Parcel
                parcelId ParcelId key
                status String
              projection ParcelProjection => Parcel
                from ParcelPacked
                  status = "packed"

        workflow Pickup
          for Parcel
          trigger Packed on ParcelPacked
            -> Book
          action Book invokes Carrier.BookPickup attempts 2
            parcelId = parcelId
            address = "x"
            next -> Tell
            NoCapacity -> Done
            failed -> Done
          compute Risk
            returns
              score String
            ```csharp
            return new { score = "low" };
            ```
            next -> Done
          action Tell invokes Chat.Post
            channel = "#c"
            text = Book.trackingNumber
            next -> Done
          decide Check when Risk.score == "low"
            "a" -> Done
            "b" -> Done
          action Done invokes Chat.Post
            channel = "#c"
            text = "done"
        """;

    string _refusal;

    void Because()
    {
        try
        {
            property_order_binding.AdmitScreens(Source);
        }
        catch (InvalidSemanticContract exception)
        {
            _refusal = exception.Message;
        }
    }

    [Fact] void should_refuse_the_node_result_read_naming_the_issue() => (_refusal?.Contains("#1252", StringComparison.Ordinal) == true && _refusal.Contains("Check", StringComparison.Ordinal)).ShouldBeTrue();
}
#endif

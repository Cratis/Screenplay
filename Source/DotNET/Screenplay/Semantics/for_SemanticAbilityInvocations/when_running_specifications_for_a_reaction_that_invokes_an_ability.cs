// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_running_specifications_for_a_reaction_that_invokes_an_ability : Specification
{
    const string Parcel = "9c858901-8a57-4791-81fe-4c455b099bc9";

    const string Source =
        """
        concept ParcelId : Uuid
        concept TrackingNumber : String

        system Carrier
          ability BookPickup idempotent
            parcelId ParcelId
            returns
              trackingNumber TrackingNumber
            outcome NoCapacity

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
              specification TheCarrierIsNotAsked
                when append OrderPacked
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  parcelId = "9c858901-8a57-4791-81fe-4c455b099bc9"
                then PickupBooked
                  for "9c858901-8a57-4791-81fe-4c455b099bc9"
                  trackingNumber = "TRK-1"
        """;

    Dictionary<string, SemanticSpecificationRun> _runs;

    void Because()
    {
        var compilation = property_order_binding.AdmitScreens(Source).Compilation!;
        var runner = new SemanticSpecificationRunner();
        _runs = compilation.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices)
            .SelectMany(slice => slice.Specifications)
            .ToDictionary(specification => specification.Name, specification => runner.Run(compilation, specification.Id));
    }

    [Fact] void should_pass_when_the_ability_returns_what_the_branch_reads() => _runs["TheCarrierReturnsATrackingNumber"].Passed.ShouldBeTrue();
    [Fact] void should_record_the_invocation_with_its_inputs() => _runs["TheCarrierReturnsATrackingNumber"].Invocations.Single().Inputs.Single().Name.ShouldEqual("parcelId");
    [Fact] void should_pass_when_the_ability_answers_a_declared_outcome() => _runs["TheCarrierHasNoCapacity"].Passed.ShouldBeTrue();
    [Fact] void should_pass_when_the_ability_fails() => _runs["TheCarrierFails"].Passed.ShouldBeTrue();
    [Fact] void should_not_prove_a_branch_that_reads_an_answer_nobody_gave() => _runs["TheCarrierIsNotAsked"].Execution.ShouldBeOfExactType<SemanticUnsupported>();
}
#endif

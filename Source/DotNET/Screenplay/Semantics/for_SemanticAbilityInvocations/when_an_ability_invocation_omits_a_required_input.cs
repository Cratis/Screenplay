// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_ability_invocation_omits_a_required_input : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(_invocation with { Inputs = [] });

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_name_the_target() => _error!.Message.ShouldEqual("Reaction 'PickupBooking' must give 'Carrier.BookPickup' every required input.");
}
#endif

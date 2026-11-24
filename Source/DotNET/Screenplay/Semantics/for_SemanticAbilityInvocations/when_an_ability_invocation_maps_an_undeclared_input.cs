// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_ability_invocation_maps_an_undeclared_input : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(_invocation with { Inputs = [_invocation.Inputs[0] with { Input = "ParcelId" }] });

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_compare_input_names_case_sensitively() => _error!.Message.ShouldEqual("Reaction 'PickupBooking' gives 'Carrier.BookPickup' an input it does not declare.");
}
#endif

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_ability_invocation_repeats_a_branch : given.an_ability_invocation_model
{
    void Because()
    {
        var invocation = WithBranchValue(0, new("result", "missing", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)));
        _error = Rebuild(invocation with { Branches = [_invocation.Branches[0], invocation.Branches[0]] });
    }

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_check_the_selector_before_its_productions() => _error!.Message.ShouldEqual("Reaction 'PickupBooking' handles 'returned' of 'Carrier.BookPickup' more than once or not at all declared.");
}
#endif

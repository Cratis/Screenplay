// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_ability_invocation_carries_command_mappings : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(_invocation with { Mappings = [new(default, _invocation.Inputs[0].Source)] });

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_name_the_mixed_invocation() => _error!.Message.ShouldEqual("Reaction 'PickupBooking' invokes an ability, which carries no command, command mappings or refusal branches.");
}
#endif

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_ability_invocation_also_names_a_command : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(_invocation with { Command = _invocation.System, System = default, Inputs = default });

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_check_command_members_before_the_target_and_inputs() => _error!.Message.ShouldEqual("Reaction 'PickupBooking' invokes an ability, which carries no command, command mappings or refusal branches.");
}
#endif

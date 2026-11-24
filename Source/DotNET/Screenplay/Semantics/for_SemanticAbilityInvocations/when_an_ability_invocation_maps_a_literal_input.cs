// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_ability_invocation_maps_a_literal_input : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(_invocation with
    {
        Inputs = [_invocation.Inputs[0] with { Source = new SemanticValueExpression(SemanticValue.Text("9c858901-8a57-4791-81fe-4c455b099bc9")) }]
    });

    [Fact] void should_accept_the_value_as_the_declared_concept() => _error.ShouldBeNull();
}
#endif

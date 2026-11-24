// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_invocation_reads_a_previous_invocations_failure : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(_invocation, _invocation with
    {
        Inputs = [_invocation.Inputs[0] with { Source = new SemanticInvocationValueExpression("failure", "reason", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)) }]
    });

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_restore_the_branch_scope_before_the_next_invocation() => _error!.Message.ShouldEqual("'$failure' is not available here or has a type its source does not declare.");
}
#endif

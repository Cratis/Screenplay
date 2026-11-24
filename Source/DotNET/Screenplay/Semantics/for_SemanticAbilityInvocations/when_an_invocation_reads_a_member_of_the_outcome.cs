// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_invocation_reads_a_member_of_the_outcome : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(WithBranchValue(1, new("outcome", "name", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))));

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_allow_only_the_bare_outcome() => _error!.Message.ShouldEqual("'$outcome' is not available here or has a type its source does not declare.");
}
#endif

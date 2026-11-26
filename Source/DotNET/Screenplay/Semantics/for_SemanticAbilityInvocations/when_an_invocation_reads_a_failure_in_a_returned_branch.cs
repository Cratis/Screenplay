// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_invocation_reads_a_failure_in_a_returned_branch : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(WithBranchValue(0, new("failure", "reason", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))));

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_require_the_failed_branch() => _error!.Message.ShouldEqual("'$failure' is not available here or has a type its source does not declare.");
}
#endif

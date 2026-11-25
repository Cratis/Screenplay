// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_invocation_result_claims_the_wrong_type : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(WithBranchValue(0, new("result", "trackingNumber", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))));

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_require_the_exact_returned_concept() => _error!.Message.ShouldEqual("'$result' is not available here or has a type its source does not declare.");
}
#endif

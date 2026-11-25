// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_invocation_reads_an_unknown_value_source : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(WithBranchValue(0, new("Result", "trackingNumber", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))));

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_compare_sources_case_sensitively() => _error!.Message.ShouldEqual("'$Result' is not available here or has a type its source does not declare.");
}
#endif

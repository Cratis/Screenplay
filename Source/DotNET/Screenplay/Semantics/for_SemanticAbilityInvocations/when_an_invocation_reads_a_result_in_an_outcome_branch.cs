// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_invocation_reads_a_result_in_an_outcome_branch : given.an_ability_invocation_model
{
    void Because()
    {
        var value = (SemanticInvocationValueExpression)_invocation.Branches[0].Produces.Single().Mappings.Single().Source;
        _error = Rebuild(WithBranchValue(1, value));
    }

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_require_the_returned_branch() => _error!.Message.ShouldEqual("'$result' is not available here or has a type its source does not declare.");
}
#endif

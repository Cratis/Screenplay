// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_invocation_reads_the_outcome_name : given.an_ability_invocation_model
{
    void Because()
    {
        var invocation = WithBranchValue(2, new("outcome", "", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text)));
        _error = Rebuild(invocation with { Branches = [invocation.Branches[2] with { Selector = "NoCapacity" }] });
    }

    [Fact] void should_accept_the_outcome_as_text() => _error.ShouldBeNull();
}
#endif

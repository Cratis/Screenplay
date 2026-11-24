// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_invocation_reads_the_failure_message : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(WithBranchValue(2, new("failure", "message", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))));

    [Fact] void should_accept_the_failure_message_as_text() => _error.ShouldBeNull();
}
#endif

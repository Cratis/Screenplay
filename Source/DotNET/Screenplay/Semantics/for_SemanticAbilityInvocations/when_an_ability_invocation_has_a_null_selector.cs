// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_an_ability_invocation_has_a_null_selector : given.an_ability_invocation_model
{
    void Because() => _error = Rebuild(_invocation with { Branches = [new(null!, [])] });

    [Fact] void should_preserve_admission_of_an_empty_null_selected_branch() => _error.ShouldBeNull();
}
#endif

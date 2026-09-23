// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_checking_whether_a_value_is_empty;

// An unknown variant used to be treated as present, silently passing a 'not empty' rule.
public class with_an_unknown_value_variant : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => SemanticEvaluator.IsEmpty(new an_unknown_value()));

    [Fact] void should_fail_as_a_malformed_value() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_say_the_variant_is_unknown() => _error.Message.ShouldContain("unknown");

    sealed record an_unknown_value() : SemanticValue(SemanticValueKind.Text);
}

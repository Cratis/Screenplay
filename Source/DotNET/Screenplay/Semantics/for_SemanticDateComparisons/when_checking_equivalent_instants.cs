// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_equivalent_instants : Specification
{
    SemanticRuleCheck _result;

    void Because() => _result = SemanticDateComparisons.Check(
        new(default, SemanticValidationRuleKind.Equal, SemanticValue.Text("2026-01-01T00:00:00.0000000Z"), "The instants must agree."),
        SemanticValue.Text("2026-01-01T02:00:00+02:00"),
        SemanticPrimitiveType.DateTime);

    [Fact] void should_compare_instants_rather_than_text() => _result.Status.ShouldEqual(SemanticRuleStatus.Satisfied);
    [Fact] void should_preserve_the_authored_message_even_on_success() => _result.Message.ShouldEqual("The instants must agree.");
}

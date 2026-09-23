// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_rule_quantifies_over_a_collection : given.a_validated_command
{
    void Because() => _result = BindRules("weights all > 0 message \"Every weight counts\"", "weights all >= 0.5");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_all_greater_than() => Command.Validations[0].Kind.ShouldEqual(SemanticValidationRuleKind.AllGreaterThan);
    [Fact] void should_bind_all_greater_than_or_equal() => Command.Validations[1].Kind.ShouldEqual(SemanticValidationRuleKind.AllGreaterThanOrEqual);
    [Fact] void should_bind_an_element_operand() => Command.Validations[1].Operand.ShouldEqual(SemanticValue.Number(0.5m));
    [Fact] void should_keep_the_message() => Command.Validations[0].Message.ShouldEqual("Every weight counts");
}

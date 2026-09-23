// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_rule_orders_a_number : given.a_validated_command
{
    void Because() => _result = BindRules("amount > 0 message \"An order is for something\"", "quantity >= 1", "quantity < 100", "amount <= 1000.5");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_greater_than() => Command.Validations[0].Kind.ShouldEqual(SemanticValidationRuleKind.GreaterThan);
    [Fact] void should_bind_the_operand() => Command.Validations[0].Operand.ShouldEqual(SemanticValue.Number(0));
    [Fact] void should_keep_the_message() => Command.Validations[0].Message.ShouldEqual("An order is for something");
    [Fact] void should_bind_greater_than_or_equal() => Command.Validations[1].Kind.ShouldEqual(SemanticValidationRuleKind.GreaterThanOrEqual);
    [Fact] void should_bind_less_than() => Command.Validations[2].Kind.ShouldEqual(SemanticValidationRuleKind.LessThan);
    [Fact] void should_bind_less_than_or_equal() => Command.Validations[3].Kind.ShouldEqual(SemanticValidationRuleKind.LessThanOrEqual);
    [Fact] void should_bind_a_decimal_operand() => Command.Validations[3].Operand.ShouldEqual(SemanticValue.Number(1000.5m));
}

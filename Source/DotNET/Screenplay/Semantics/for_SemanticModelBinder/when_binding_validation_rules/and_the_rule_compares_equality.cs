// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_rule_compares_equality : given.a_validated_command
{
    void Because() => _result = BindRules("status != closed", "name == \"Screenplay\"", "express == true", "amount != 0");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_an_enumeration_member_as_its_text() => Command.Validations[0].Operand.ShouldEqual(SemanticValue.Text("closed"));
    [Fact] void should_bind_inequality() => Command.Validations[0].Kind.ShouldEqual(SemanticValidationRuleKind.NotEqual);
    [Fact] void should_bind_equality_on_text() => Command.Validations[1].Kind.ShouldEqual(SemanticValidationRuleKind.Equal);
    [Fact] void should_bind_the_text_operand() => Command.Validations[1].Operand.ShouldEqual(SemanticValue.Text("Screenplay"));
    [Fact] void should_bind_the_boolean_operand() => Command.Validations[2].Operand.ShouldEqual(SemanticValue.Boolean(true));
    [Fact] void should_bind_the_number_operand() => Command.Validations[3].Operand.ShouldEqual(SemanticValue.Number(0));
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

// A bound on text constrains its length, so the operand is a whole number even though the property is text.
public class and_a_bound_limits_text_length : given.a_validated_command
{
    void Because() => _result = BindRules("name max 500 message \"Too long\"", "reference min 2");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_the_maximum() => Command.Validations[0].Kind.ShouldEqual(SemanticValidationRuleKind.Maximum);
    [Fact] void should_bind_the_maximum_length() => Command.Validations[0].Operand.ShouldEqual(SemanticValue.Number(500));
    [Fact] void should_bind_the_maximum_to_its_property() => Command.Validations[0].Property.ShouldEqual(PropertyId("name"));
    [Fact] void should_keep_the_message() => Command.Validations[0].Message.ShouldEqual("Too long");
    [Fact] void should_bind_the_minimum() => Command.Validations[1].Kind.ShouldEqual(SemanticValidationRuleKind.Minimum);
    [Fact] void should_bind_the_minimum_length() => Command.Validations[1].Operand.ShouldEqual(SemanticValue.Number(2));
}

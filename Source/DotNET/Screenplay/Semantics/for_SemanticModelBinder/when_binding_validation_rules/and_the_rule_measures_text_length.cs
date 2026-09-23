// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

// A length rule sits on text but its operand is a whole number - operand typing follows the kind.
public class and_the_rule_measures_text_length : given.a_validated_command
{
    void Because() => _result = BindRules("reference length == 10 message \"A reference has ten characters\"");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_the_length_kind() => Rule.Kind.ShouldEqual(SemanticValidationRuleKind.Length);
    [Fact] void should_bind_a_whole_number_operand() => Rule.Operand.ShouldEqual(SemanticValue.Number(10));
    [Fact] void should_keep_the_message() => Rule.Message.ShouldEqual("A reference has ten characters");
}

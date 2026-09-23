// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_a_bound_limits_a_number : given.a_validated_command
{
    void Because() => _result = BindRules("quantity min 1", "amount max 999.5");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_the_minimum() => Command.Validations[0].Kind.ShouldEqual(SemanticValidationRuleKind.Minimum);
    [Fact] void should_bind_the_minimum_value() => Command.Validations[0].Operand.ShouldEqual(SemanticValue.Number(1));
    [Fact] void should_bind_the_maximum() => Command.Validations[1].Kind.ShouldEqual(SemanticValidationRuleKind.Maximum);
    [Fact] void should_bind_the_maximum_value() => Command.Validations[1].Operand.ShouldEqual(SemanticValue.Number(999.5m));
}

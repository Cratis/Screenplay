// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_number_literals;

public class with_small_numbers : given.a_validated_command
{
    void Because() => _result = BindRules("quantity min 42", "quantity max -42", "amount min 0.5", "amount max -0.5", "quantity min 9007199254740991");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_select_language_v10() => _result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V10);
    [Fact] void should_select_semantic_v10() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
    [Fact] void should_keep_a_small_positive_integer() => Command.Validations[0].Operand.ShouldEqual(SemanticValue.Number(42));
    [Fact] void should_keep_a_small_negative_integer() => Command.Validations[1].Operand.ShouldEqual(SemanticValue.Number(-42));
    [Fact] void should_keep_a_positive_fraction() => Command.Validations[2].Operand.ShouldEqual(SemanticValue.Number(0.5m));
    [Fact] void should_keep_a_negative_fraction() => Command.Validations[3].Operand.ShouldEqual(SemanticValue.Number(-0.5m));
    [Fact] void should_keep_the_selecting_integer_exactly() => Command.Validations[4].Operand.ShouldEqual(SemanticValue.Number(9007199254740991m));
}

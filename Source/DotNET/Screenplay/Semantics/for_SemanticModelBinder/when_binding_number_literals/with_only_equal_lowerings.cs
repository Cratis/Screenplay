// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_number_literals;

public class with_only_equal_lowerings : given.a_validated_command
{
    void Because() => _result = BindRules("amount min 0.5", "quantity min 42");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_language_v1() => _result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V1);
    [Fact] void should_keep_semantic_v1() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_keep_the_fraction() => Command.Validations[0].Operand.ShouldEqual(SemanticValue.Number(0.5m));
    [Fact] void should_keep_the_integer() => Command.Validations[1].Operand.ShouldEqual(SemanticValue.Number(42));
    [Fact] void should_not_admit_v10_without_a_selecting_construct() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, _result.Value!.Model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
}

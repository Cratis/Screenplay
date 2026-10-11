// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_number_literals;

public class with_a_seventeen_digit_fraction : given.a_validated_command
{
    void Because() => _result = BindRules("amount min 0.10000000000000002");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_select_language_v10() => _result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V10);
    [Fact] void should_select_semantic_v10() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
    [Fact] void should_keep_the_shortest_round_tripping_decimal() => Rule.Operand.ShouldEqual(SemanticValue.Number(0.10000000000000002m));
}

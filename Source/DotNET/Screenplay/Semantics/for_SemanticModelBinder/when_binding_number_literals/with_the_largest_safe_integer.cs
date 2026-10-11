// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_number_literals;

public class with_the_largest_safe_integer : given.a_validated_command
{
    void Because() => _result = BindRules("quantity min 9007199254740991");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_select_language_v10() => _result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V10);
    [Fact] void should_select_semantic_v10() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
    [Fact] void should_keep_the_integer_exactly() => Rule.Operand.ShouldEqual(SemanticValue.Number(9007199254740991m));
    [Fact] void should_preserve_the_model_on_round_trip() => SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model)).Revision.ShouldEqual(_result.Value.Model.Revision);
}

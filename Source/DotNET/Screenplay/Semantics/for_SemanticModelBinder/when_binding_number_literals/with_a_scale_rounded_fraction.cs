// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_number_literals;

public class with_a_scale_rounded_fraction : given.a_validated_command
{
    void Because() => _result = BindRules("amount min 0.00000000000000012345678901235");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_language_v1() => _result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V1);
    [Fact] void should_keep_semantic_v1() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_keep_the_legacy_lowering() => Rule.Operand.ShouldEqual(SemanticValue.Number(0.0000000000000001234567890123m));
    [Fact] void should_preserve_the_model_on_round_trip() => SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model)).Revision.ShouldEqual(_result.Value.Model.Revision);
}

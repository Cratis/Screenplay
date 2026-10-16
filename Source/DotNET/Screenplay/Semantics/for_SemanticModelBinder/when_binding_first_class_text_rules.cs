// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_first_class_text_rules : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("concept Label : String\n  validate\n    contains \"A\"\n    not contains \"B\"\n    starts with \"A\"\n    ends with \"A\"\n");

    [Fact] void should_bind() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(value => value.Message)));
    [Fact] void should_select_the_text_rule_version() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V24);
    [Fact] void should_preserve_the_four_distinct_kinds() => _result.Value!.Model.Application.Concepts.Single().Validations.Select(rule => rule.Kind).ShouldEqual([SemanticValidationRuleKind.Contains, SemanticValidationRuleKind.NotContains, SemanticValidationRuleKind.StartsWith, SemanticValidationRuleKind.EndsWith]);
    [Fact] void should_round_trip_canonical_bytes() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model))).ShouldEqual(SemanticModelSerializer.Serialize(_result.Value!.Model));
    [Fact] void should_reject_a_numeric_subject() => Bind("concept Label : Integer\n  validate\n    contains \"A\"\n").Success.ShouldBeFalse();
    [Fact] void should_reject_an_enumeration_subject() => Bind("concept Label enum\n  A\n  B\n  validate\n    contains \"A\"\n").Success.ShouldBeFalse();
    [Fact] void should_reject_a_null_operand() => Bind("concept Label : String\n  validate\n    contains null\n").Success.ShouldBeFalse();
    [Fact] void should_reject_a_numeric_operand() => Bind("concept Label : String\n  validate\n    contains 42\n").Success.ShouldBeFalse();
    [Fact] void should_keep_old_models_on_their_existing_version() => Bind("concept Label : String\n  validate\n    matches \"A\"\n").Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_refuse_new_kinds_in_an_old_contract() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, _result.Value!.Model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_concept_validation_rules;

public class and_not_empty_targets_text : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("concept Reference : String\n  validate\n    not empty");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_the_rule() => _result.Value!.Model.Application.Concepts.Single().Validations.Single().Kind.ShouldEqual(SemanticValidationRuleKind.NotEmpty);
}

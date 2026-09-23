// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_concept_validation_rules;

public class and_the_rule_compares_an_enumeration_member : given.a_semantic_binder
{
    const string Source =
        """
        concept Status : Enum
          open
          closed
          validate
            != closed message "Closed is not a status to start with"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_inequality() => Rule.Kind.ShouldEqual(SemanticValidationRuleKind.NotEqual);
    [Fact] void should_bind_the_member_as_its_text() => Rule.Operand.ShouldEqual(SemanticValue.Text("closed"));

    SemanticValidationRule Rule => _result.Value!.Model.Application.Concepts.Single().Validations.Single();
}

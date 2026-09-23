// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_concept_validation_rules;

// One bound kind each way: its meaning follows the concept's type - a text length or a number's value.
public class and_a_bound_limits_the_value : given.a_semantic_binder
{
    const string Source =
        """
        concept Reference : String
          validate
            max 20 message "A reference is short"
        concept Quantity : Int
          validate
            min 1
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_the_text_bound() => Rule("Reference").Kind.ShouldEqual(SemanticValidationRuleKind.Maximum);
    [Fact] void should_bind_the_text_bound_as_a_length() => Rule("Reference").Operand.ShouldEqual(SemanticValue.Number(20));
    [Fact] void should_keep_the_message() => Rule("Reference").Message.ShouldEqual("A reference is short");
    [Fact] void should_bind_the_number_bound() => Rule("Quantity").Kind.ShouldEqual(SemanticValidationRuleKind.Minimum);
    [Fact] void should_bind_the_number_bound_value() => Rule("Quantity").Operand.ShouldEqual(SemanticValue.Number(1));
    [Fact] void should_use_the_implicit_concept_value() => Rule("Quantity").Property.IsSet.ShouldBeFalse();

    SemanticValidationRule Rule(string concept) => _result.Value!.Model.Application.Concepts.Single(_ => _.Name == concept).Validations.Single();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

// A length operand is a whole number on a text property, so operand typing follows the rule kind.
public class when_measuring_text_length : a_valid_semantic_model
{
    Exception _onText;
    Exception _onNumber;
    Exception _onEnumeration;
    Exception _withTextOperand;
    Exception _withNegativeOperand;

    void Because()
    {
        _onText = Validate(SemanticPrimitiveType.Text, [], SemanticValue.Number(10));
        _onNumber = Validate(SemanticPrimitiveType.WholeNumber, [], SemanticValue.Number(10));
        _onEnumeration = Validate(SemanticPrimitiveType.Text, ["open"], SemanticValue.Number(4));
        _withTextOperand = Validate(SemanticPrimitiveType.Text, [], SemanticValue.Text("10"));
        _withNegativeOperand = Validate(SemanticPrimitiveType.Text, [], SemanticValue.Number(-1));
    }

    Exception Validate(SemanticPrimitiveType primitive, string[] values, SemanticValue operand)
    {
        var concept = new SemanticConcept(
            Id(SemanticKind.Concept, "Measured"),
            "Measured",
            primitive,
            [.. values],
            [new(default, SemanticValidationRuleKind.Length, operand, null)]);
        return Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            _application with { Concepts = [.. _application.Concepts, concept] }));
    }

    [Fact] void should_admit_a_whole_number_length_on_text() => _onText.ShouldBeNull();
    [Fact] void should_reject_a_length_on_a_number() => _onNumber.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_length_on_an_enumeration() => _onEnumeration.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_text_operand() => _withTextOperand.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_negative_operand() => _withNegativeOperand.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_ordering_validation_by_property_type : a_valid_semantic_model
{
    Exception _onNumber;
    Exception _onText;
    Exception _withTextOperand;

    void Because()
    {
        _onNumber = Validate(SemanticPrimitiveType.WholeNumber, SemanticValidationRuleKind.GreaterThan, SemanticValue.Number(0));
        _onText = Validate(SemanticPrimitiveType.Text, SemanticValidationRuleKind.LessThan, SemanticValue.Text("m"));
        _withTextOperand = Validate(SemanticPrimitiveType.DecimalNumber, SemanticValidationRuleKind.GreaterThanOrEqual, SemanticValue.Text("0"));
    }

    Exception Validate(SemanticPrimitiveType primitive, SemanticValidationRuleKind kind, SemanticValue operand)
    {
        var concept = new SemanticConcept(Id(SemanticKind.Concept, "Ordered"), "Ordered", primitive, [], [new(default, kind, operand, null)]);
        return Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            _application with { Concepts = [.. _application.Concepts, concept] }));
    }

    [Fact] void should_admit_ordering_on_a_number() => _onNumber.ShouldBeNull();
    [Fact] void should_reject_ordering_on_text() => _onText.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_operand_of_another_type() => _withTextOperand.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

// A quantified rule sits on a collection, and its operand is typed by the element type.
public class when_quantifying_validation_over_a_collection : a_valid_semantic_model
{
    Exception _onNumbers;
    Exception _onScalar;
    Exception _onText;
    Exception _withCollectionOperand;

    void Because()
    {
        _onNumbers = Validate(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber, isCollection: true), SemanticValue.Number(0));
        _onScalar = Validate(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), SemanticValue.Number(0));
        _onText = Validate(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isCollection: true), SemanticValue.Text("a"));
        _withCollectionOperand = Validate(SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber, isCollection: true), SemanticValue.Array([SemanticValue.Number(0)]));
    }

    Exception Validate(SemanticTypeReference type, SemanticValue operand)
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var weights = new SemanticProperty(Id(SemanticKind.Property, "RegisterProject.Weights"), "Weights", type, false);
        var command = slice.Commands.Single() with
        {
            Properties = [.. slice.Commands.Single().Properties, weights],
            Validations = [new(weights.Id, SemanticValidationRuleKind.AllGreaterThan, operand, null)]
        };
        return Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Commands = [command], Specifications = [] })));
    }

    [Fact] void should_admit_a_collection_of_numbers() => _onNumbers.ShouldBeNull();
    [Fact] void should_reject_a_scalar() => _onScalar.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_collection_of_text() => _onText.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_collection_operand() => _withCollectionOperand.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValueValidator.when_revalidating_a_memoized_composite;

public class with_a_new_invalid_reference : Specification
{
    SemanticValueValidator _validator;
    SemanticTypeReference _target;
    SemanticValue _invalid;
    Exception? _exception;

    void Establish()
    {
        var typeId = SemanticId.Create(SemanticKind.CompositeType, "Line");
        var quantityId = SemanticId.Create(SemanticKind.Property, "quantity");
        _target = SemanticTypeReference.ForCompositeType(typeId);
        var type = new SemanticCompositeType(typeId, "Line", [new(quantityId, "quantity", SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), false)]);
        _validator = new(new Dictionary<SemanticId, SemanticConcept>(), new Dictionary<SemanticId, SemanticCompositeType> { [typeId] = type }, memoize: true);
        var valid = new SemanticCompositeValue([new(quantityId, SemanticValue.Number(1))]);
        _validator.Validate(valid, _target, "line");
        _invalid = valid with { Properties = [new(quantityId, SemanticValue.Number(1.5m))] };
    }

    void Because() => _exception = Catch.Exception(() => _validator.Validate(_invalid, _target, "line"));

    [Fact] void should_validate_the_new_reference() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_keep_the_property_failure_message() => _exception!.Message.ShouldEqual("A property 'quantity' of line is incompatible with its target type.");
}

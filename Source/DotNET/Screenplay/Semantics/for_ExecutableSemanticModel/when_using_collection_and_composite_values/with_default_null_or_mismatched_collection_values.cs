// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_using_collection_and_composite_values;

public class with_default_null_or_mismatched_collection_values : given.a_value_algebra_model
{
    Exception _arrayForScalar;
    Exception _defaultArray;
    Exception _defaultObject;
    Exception _nestedArray;
    Exception _nullElement;
    Exception _nullObjectProperty;
    Exception _nullReferenceElement;
    Exception _scalarForArray;

    void Because()
    {
        _defaultArray = ValidateBackups(new SemanticArrayValue(default));
        _defaultObject = ValidatePayload(new SemanticCompositeValue(default));
        _nullElement = ValidateBackups(SemanticValue.Array([SemanticValue.Null]));
        _nullReferenceElement = ValidateBackups(new SemanticArrayValue([null!]));
        _nullObjectProperty = ValidatePayload(new SemanticCompositeValue([null!]));
        _scalarForArray = ValidateBackups(SemanticValue.Text("not an array"));

        var scalarChildProperty = SemanticValue.Composite(
        [
            new(_childNamePropertyId, SemanticValue.Array([SemanticValue.Text("not scalar")]))
        ]);
        _arrayForScalar = ValidateBackups(SemanticValue.Array([scalarChildProperty]));
        _nestedArray = ValidateBackups(SemanticValue.Array([SemanticValue.Array([])]));
    }

    [Fact] void should_reject_a_default_array() => _defaultArray.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_default_object_property_set() => _defaultObject.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_semantic_null_collection_element() => _nullElement.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_null_reference_collection_element() => _nullReferenceElement.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_null_reference_object_property() => _nullObjectProperty.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_scalar_for_a_collection_target() => _scalarForArray.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_array_for_a_scalar_target() => _arrayForScalar.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_nested_array_for_a_single_collection_cardinality() => _nestedArray.ShouldBeOfExactType<InvalidSemanticContract>();

    Exception ValidateBackups(SemanticValue backups) => Catch.Exception(() => CreateModel(_validPayload, backups));
    Exception ValidatePayload(SemanticValue payload) => Catch.Exception(() => CreateModel(payload, _validBackups));
}
#endif

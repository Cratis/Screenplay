// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_using_collection_and_composite_values;

public class with_duplicate_missing_unknown_or_null_object_properties : given.a_value_algebra_model
{
    Exception _duplicate;
    Exception _missing;
    Exception _requiredNull;
    Exception _unknown;

    void Because()
    {
        _duplicate = Validate(SemanticValue.Composite(
        [
            new(_payloadItemsPropertyId, SemanticValue.Array([])),
            new(_payloadItemsPropertyId, SemanticValue.Array([])),
            new(_labelsPropertyId, SemanticValue.Array([]))
        ]));
        _missing = Validate(SemanticValue.Composite(
        [
            new(_labelsPropertyId, SemanticValue.Array([]))
        ]));
        _unknown = Validate(SemanticValue.Composite(
        [
            new(_payloadItemsPropertyId, SemanticValue.Array([])),
            new(_labelsPropertyId, SemanticValue.Array([])),
            new(Id(999), SemanticValue.Text("unknown"))
        ]));
        _requiredNull = Validate(SemanticValue.Composite(
        [
            new(_payloadItemsPropertyId, SemanticValue.Null),
            new(_labelsPropertyId, SemanticValue.Array([]))
        ]));
    }

    [Fact] void should_reject_duplicate_object_properties() => _duplicate.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_missing_required_object_properties() => _missing.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_unknown_object_properties() => _unknown.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_null_for_a_required_object_property() => _requiredNull.ShouldBeOfExactType<InvalidSemanticContract>();

    Exception Validate(SemanticValue payload) => Catch.Exception(() => CreateModel(payload, _validBackups));
}
#endif

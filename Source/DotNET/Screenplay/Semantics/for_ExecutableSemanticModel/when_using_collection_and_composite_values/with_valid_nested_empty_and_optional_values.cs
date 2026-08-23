// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel.when_using_collection_and_composite_values;

public class with_valid_nested_empty_and_optional_values : given.a_value_algebra_model
{
    Exception _excessiveDepthError;
    Exception _literalError;
    Exception _optionalMissingError;
    Exception _optionalNullError;
    Exception _resolvedError;

    void Because()
    {
        _resolvedError = Catch.Exception(() => CreateModel(_validPayload, _validBackups));
        _literalError = Catch.Exception(() => CreateModel(
            _validPayload,
            _validBackups,
            SemanticExpression.FromValue(_validPayload),
            SemanticExpression.FromValue(_validBackups)));

        var withoutOptional = SemanticValue.Composite(
        [
            new(_labelsPropertyId, SemanticValue.Array([])),
            new(_payloadItemsPropertyId, SemanticValue.Array([]))
        ]);
        _optionalMissingError = Catch.Exception(() => CreateModel(withoutOptional, SemanticValue.Null));

        var withNullOptional = SemanticValue.Composite(
        [
            new(_payloadPreferredPropertyId, SemanticValue.Null),
            new(_labelsPropertyId, SemanticValue.Array([])),
            new(_payloadItemsPropertyId, SemanticValue.Array([]))
        ]);
        _optionalNullError = Catch.Exception(() => CreateModel(withNullOptional, SemanticValue.Null));
        _excessiveDepthError = Catch.Exception(() => given.recursive_value_model.Create(
            given.recursive_value_model.CreateValue(Serialization.CanonicalJson.MaximumDepth + 1)));
    }

    [Fact] void should_accept_nested_ordered_values_and_empty_collections() => _resolvedError.ShouldBeNull();
    [Fact] void should_allow_literal_expressions_to_carry_typed_values() => _literalError.ShouldBeNull();
    [Fact] void should_allow_optional_object_properties_to_be_missing() => _optionalMissingError.ShouldBeNull();
    [Fact] void should_distinguish_a_null_optional_collection_and_property_from_missing_required_values() => _optionalNullError.ShouldBeNull();
    [Fact] void should_reject_excessive_value_depth() => _excessiveDepthError.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif

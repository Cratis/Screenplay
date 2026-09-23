// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

// Maximum and minimum follow the property's type: on text they bound its length with a whole number.
public class when_bounding_validation_by_property_type : a_valid_semantic_model
{
    Exception _textLength;
    Exception _negativeLength;
    Exception _fractionalLength;
    Exception _equalityOnIdentifier;

    void Because()
    {
        _textLength = Validate(new(_commandNamePropertyId, SemanticValidationRuleKind.Maximum, SemanticValue.Number(20), null));
        _negativeLength = Validate(new(_commandNamePropertyId, SemanticValidationRuleKind.Minimum, SemanticValue.Number(-1), null));
        _fractionalLength = Validate(new(_commandNamePropertyId, SemanticValidationRuleKind.Maximum, SemanticValue.Number(2.5m), null));
        _equalityOnIdentifier = Validate(new(_commandProjectIdPropertyId, SemanticValidationRuleKind.Equal, SemanticValue.Text("00000000-0000-0000-0000-000000000001"), null));
    }

    Exception Validate(SemanticValidationRule validation)
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single() with { Validations = [validation] };
        return Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Commands = [command] })));
    }

    [Fact] void should_admit_a_whole_number_length_on_text() => _textLength.ShouldBeNull();
    [Fact] void should_reject_a_negative_length() => _negativeLength.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_fractional_length() => _fractionalLength.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_equality_on_an_identifier() => _equalityOnIdentifier.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif

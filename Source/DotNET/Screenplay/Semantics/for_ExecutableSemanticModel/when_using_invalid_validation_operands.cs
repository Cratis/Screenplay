// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_invalid_validation_operands : a_valid_semantic_model
{
    Exception _forbidden;
    Exception _missing;
    Exception _wrongType;

    void Because()
    {
        _forbidden = Validate(new(_commandNamePropertyId, SemanticValidationRuleKind.NotEmpty, SemanticValue.Text("x"), null));
        _missing = Validate(new(_commandNamePropertyId, SemanticValidationRuleKind.Equal, null, null));
        _wrongType = Validate(new(_commandNamePropertyId, SemanticValidationRuleKind.Maximum, SemanticValue.Number(1), null));
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

    [Fact] void should_reject_a_forbidden_operand() => _forbidden.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_missing_operand() => _missing.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_incompatible_operand() => _wrongType.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif

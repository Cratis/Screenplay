// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_a_match_rule : a_valid_semantic_model
{
    Exception _valid;
    Exception _invalidPattern;
    Exception _nonTextOperand;
    Exception _nonTextTarget;

    void Because()
    {
        _valid = Check(_commandNamePropertyId, SemanticValue.Text("^name$"));
        _invalidPattern = Check(_commandNamePropertyId, SemanticValue.Text("["));
        _nonTextOperand = Check(_commandNamePropertyId, SemanticValue.Number(1));
        _nonTextTarget = Check(_commandProjectIdPropertyId, SemanticValue.Text(".*"));
    }

    Exception Check(SemanticId property, SemanticValue operand)
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0);
        var command = slice.Commands.Single() with { Validations = [new(property, SemanticValidationRuleKind.Matches, operand, null)] };
        return Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, ReplaceSlice(slice with { Commands = [command] })));
    }

    [Fact] void should_accept_a_valid_text_pattern() => _valid.ShouldBeNull();
    [Fact] void should_reject_a_malformed_pattern() => _invalidPattern.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_non_text_operand() => _nonTextOperand.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_a_non_text_target() => _nonTextTarget.ShouldBeOfExactType<InvalidSemanticContract>();
}

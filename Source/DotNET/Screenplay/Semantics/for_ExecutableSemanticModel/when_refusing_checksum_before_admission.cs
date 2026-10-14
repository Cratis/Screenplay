// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_refusing_checksum_before_admission : a_valid_semantic_model
{
    Exception _refusal;

    void Because()
    {
        var concept = _application.Concepts.First(concept => concept.Primitive == SemanticPrimitiveType.Text);
        concept = concept with { Validations = [new(default, SemanticValidationRuleKind.Checksum, SemanticValue.Text("luhn"), null)] };
        var application = _application with { Concepts = [.. _application.Concepts.Select(value => value.Id == concept.Id ? concept : value)] };
        _refusal = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, application));
    }

    [Fact] void should_fail_closed_for_a_programmatic_checksum_rule() => _refusal.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_name_the_missing_admission() => _refusal.Message.ShouldContain("admission");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_generated_concept_rules : Specification
{
    [Fact]
    void should_fail_closed_on_a_programmatic_refuted_rule()
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV20().Application;
        var concept = application.Concepts.Single(value => value.Name == "Generation5");
        var changed = concept with { Validations = [new(default, SemanticValidationRuleKind.Maximum, SemanticValue.Number(20), null) { Severity = SemanticValidationSeverity.Warning }] };
        var error = Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V20, SemanticVersion.V20, application with { Concepts = application.Concepts.SetItem(application.Concepts.IndexOf(concept), changed) }));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("witness \"---------------------\"");
    }

    [Fact]
    void should_admit_an_unproven_programmatic_rule()
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV20().Application;
        var concept = application.Concepts.Single(value => value.Name == "Generation5");
        var changed = concept with { Validations = [new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("^(?=.{21}$).*$"), null)] };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V20, SemanticVersion.V20, application with { Concepts = application.Concepts.SetItem(application.Concepts.IndexOf(concept), changed) })).ShouldBeNull();
    }

    [Fact]
    void should_refuse_v20_without_generated_concept_rules() =>
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V20, SemanticVersion.V20, canonical_serialization_golden_vectors.CreateSemanticModelV19().Application)).ShouldBeOfExactType<InvalidSemanticContract>();
}

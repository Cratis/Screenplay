// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_date_comparisons : Specification
{
    SemanticApplication _application;
    SemanticCommand _command;

    void Establish()
    {
        _application = canonical_serialization_golden_vectors.CreateSemanticModelV26().Application;
        _command = _application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
    }

    [Fact]
    void should_require_canonical_date_constants() => Refuse(_command with
    {
        Validations = [.. _command.Validations.Select(rule => rule with { Operand = SemanticValue.Text("2026-01-01T01:00:00.0000000+01:00") })]
    });

    [Fact]
    void should_require_an_explicit_offset_on_constants() => Refuse(_command with
    {
        Validations = [.. _command.Validations.Select(rule => rule with { Operand = SemanticValue.Text("2026-01-01T00:00:00.0000000") })]
    });

    [Fact]
    void should_reject_date_constants_on_the_left() => Refuse(_command with
    {
        Requirements = [.. _command.Requirements.Select(requirement => requirement with
        {
            Condition = ((SemanticComparison)requirement.Condition) with { Left = new(default, SemanticValue.Text("2026-01-01T00:00:00.0000000Z")) }
        })]
    });

    [Fact]
    void should_reject_an_empty_v26_claim()
    {
        var model = canonical_serialization_golden_vectors.CreateSemanticModel();
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V26, SemanticVersion.V26, model.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    void Refuse(SemanticCommand command)
    {
        var application = _application with
        {
            Modules = [.. _application.Modules.Select(module => module with
            {
                Features = [.. module.Features.Select(feature => feature with
                {
                    Slices = [.. feature.Slices.Select(slice => slice with { Commands = [command] })]
                })]
            })]
        };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V26, SemanticVersion.V26, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}

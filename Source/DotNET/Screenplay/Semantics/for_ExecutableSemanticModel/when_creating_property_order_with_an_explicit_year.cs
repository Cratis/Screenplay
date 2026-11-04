// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_creating_property_order_with_an_explicit_year : Specification
{
    ExecutableSemanticModel _baseline;
    ExecutableSemanticModel _model;

    void Establish() => _baseline = property_order_binding.BindDefault(property_order_binding.FutureYearSource, 3000).Value!.Model;

    void Because() => _model = ExecutableSemanticModel.Create(
        LanguageVersion.V36_1,
        SemanticVersion.V36_1,
        SemanticReadModelPropertyOrder.Apply(_baseline.Application),
        3000);

    [Fact] void should_keep_the_valid_lifecycle_base_at_36_0() => _baseline.SemanticVersion.ShouldEqual(SemanticVersion.V36);
    [Fact] void should_validate_the_inner_base_projection_at_the_explicit_year() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V36_1);
    [Fact] void should_retain_the_year_dependent_generation_pattern() => _model.Application.Concepts.Single(concept => concept.Name == "HorizonReference").Generation!.Pattern.ShouldEqual("{yyyy}-[AB]");
    [Fact] void should_retain_the_rule_requiring_the_future_year_horizon() => _model.Application.Concepts.Single(concept => concept.Name == "HorizonReference").Validations.Single().Operand.ShouldEqual(SemanticValue.Text("^3[0-9]{3}-[AB]$"));

    [Fact]
    void should_preserve_noncanonical_declaration_order()
    {
        var original = _baseline.Application.Modules[0].Features[0].Slices.SelectMany(slice => slice.ReadModels).Single();
        var preserved = _model.Application.Modules[0].Features[0].Slices.SelectMany(slice => slice.ReadModels).Single();
        SemanticId[] expected = [.. original.Properties.Select(property => property.Id)];
        preserved.PropertyOrder.ShouldEqual(expected);
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_preserving_property_order_assessment_year : Specification
{
    CompilationResult<SemanticCompilation> _baseline;
    SemanticBindingOptionsResult _result;

    void Establish()
    {
        _baseline = property_order_binding.BindDefault(property_order_binding.FutureYearSource, 3000);
    }

    void Because() => _result = property_order_binding.Bind(new(true), property_order_binding.FutureYearSource, 3000);

    [Fact] void should_keep_the_invocation_base_valid_for_the_complete_3000_to_3100_horizon() => _baseline.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V34);
    [Fact] void should_supply_the_requested_metadata() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_return_a_metadata_refusal() => _result.Refusal.ShouldBeNull();
    [Fact] void should_promote_using_the_same_assessment_year() => _result.Compilation.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V34_1);

    [Fact]
    void should_preserve_noncanonical_declaration_order()
    {
        SemanticId[] expected = [.. ReadModel(_baseline.Value!.Model).Properties.Select(property => property.Id)];
        ReadModel(_result.Compilation.Value!.Model).PropertyOrder.ShouldEqual(expected);
    }

    [Fact]
    void should_keep_default_binding_bytes_unchanged()
    {
        var baseline = property_order_binding.BindDefault();
        var defaults = property_order_binding.Bind(new());
        SemanticModelSerializer.Serialize(defaults.Compilation.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(baseline.Value!.Model));
    }

    static SemanticReadModel ReadModel(ExecutableSemanticModel model) =>
        model.Application.Modules[0].Features[0].Slices.SelectMany(slice => slice.ReadModels).Single();
}

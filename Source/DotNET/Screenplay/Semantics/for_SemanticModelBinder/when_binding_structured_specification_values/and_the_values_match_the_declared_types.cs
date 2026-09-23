// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_structured_specification_values;

public class and_the_values_match_the_declared_types : given.a_structured_specification
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Compile(Source);

    [Fact] void should_bind_the_specifications() => _result.Success.ShouldBeTrue();

    [Fact] void should_resolve_nested_composite_member_ids_and_preserve_array_order()
    {
        var model = _result.Value!.Model.Application;
        var view = model.Modules.Single().Features.Single().Slices.Single().ReadModels.Single();
        var seeding = model.Modules.Single().Features.Single().Slices.Single().Specifications.Single(specification => specification.When is null);
        var lines = (SemanticArrayValue)seeding.GivenReadModels.Single().Values.Single(value => value.TargetProperty == view.Properties.Single(property => property.Name == "lines").Id).Value;
        var lineType = model.Types.Single(type => type.Name == "Line");
        var line = (SemanticCompositeValue)lines.Values.Single();
        var sku = line.Properties.Single(value => value.TargetProperty == lineType.Properties.Single(property => property.Name == "sku").Id);
        sku.Value.ShouldEqual(SemanticValue.Text("A-1"));
        var detail = (SemanticCompositeValue)line.Properties.Single(value => value.TargetProperty == lineType.Properties.Single(property => property.Name == "detail").Id).Value;
        detail.Properties.Single().TargetProperty.ShouldEqual(model.Types.Single(type => type.Name == "Detail").Properties.Single().Id);
    }

    [Fact] void should_bind_empty_lists_and_optional_nulls()
    {
        var state = _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single(specification => specification.When is null).GivenReadModels.Single();
        state.Values.Any(value => value.Value is SemanticArrayValue { Values.IsEmpty: true }).ShouldBeTrue();
        state.Values.Any(value => value.Value is SemanticNullValue).ShouldBeTrue();
    }

    [Fact] void should_bind_structured_command_and_event_values()
    {
        var specification = _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single(specification => specification.When is not null);
        specification.When!.Values.Any(value => value.Value is SemanticArrayValue).ShouldBeTrue();
        specification.ThenEvents.Single().Values.Any(value => value.Value is SemanticArrayValue).ShouldBeTrue();
    }
}

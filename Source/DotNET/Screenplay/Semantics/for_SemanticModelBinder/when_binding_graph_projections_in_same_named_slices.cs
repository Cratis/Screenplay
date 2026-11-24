// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_graph_projections_in_same_named_slices : given.a_graph_projection_source
{
    ExecutableSemanticModel _model;

    void Because() => _model = Compile(Source + "\nmodule AnotherOrganization\n  feature OtherChart\n    slice StateView Overview\n" + Projection.Replace("projection BuildPeople", "projection BuildOtherPeople") + "\n");

    [Fact] void should_keep_both_projections() => _model.Application.GraphProjections.Length.ShouldEqual(2);
    [Fact] void should_name_each_declaring_slice() => _model.Application.GraphProjections.Select(projection => projection.DeclaringSlice).ShouldEqual(
        _model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Select(slice => (SemanticId?)slice.Id));
    [Fact] void should_distinguish_the_same_named_slices() => _model.Application.GraphProjections.Select(projection => projection.DeclaringSlice).Distinct().Count().ShouldEqual(2);
}

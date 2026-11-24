// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_graph_projection_declaring_slice : given.a_graph_projection_source
{
    ExecutableSemanticModel _model;

    void Because() => _model = Compile(Source);

    [Fact] void should_name_the_owning_slice() => _model.Application.GraphProjections.Single().DeclaringSlice.ShouldEqual(_model.Application.Modules.Single().Features.Single().Slices.Single().Id);
    [Fact] void should_select_v60() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V60);
    [Fact] void should_select_language_v60() => _model.LanguageVersion.ShouldEqual(LanguageVersion.V60);
}

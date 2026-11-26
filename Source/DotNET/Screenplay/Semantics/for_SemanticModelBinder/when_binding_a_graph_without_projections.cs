// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_graph_without_projections : given.a_graph_projection_source
{
    ExecutableSemanticModel _model;

    void Because() => _model = Compile(Source.Replace(Projection, string.Empty));

    [Fact] void should_leave_the_existing_version_unchanged() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V38);
    [Fact] void should_have_no_graph_projections() => _model.Application.GraphProjections.ShouldBeEmpty();
}

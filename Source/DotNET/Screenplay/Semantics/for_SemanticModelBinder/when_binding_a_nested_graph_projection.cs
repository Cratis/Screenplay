// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_nested_graph_projection : given.a_graph_projection_source
{
    ExecutableSemanticModel _model;

    void Because() => _model = Compile("command reads protected\nmodule Organization\n  feature Parent\n" + string.Join('\n', Source.Split('\n').Skip(2).Select(line => "  " + line)));

    [Fact] void should_name_the_declaring_slice() => _model.Application.GraphProjections.Single().DeclaringSlice.ShouldEqual(_model.Application.Modules.Single().Features.Single().Features.Single().Slices.Single().Id);
}

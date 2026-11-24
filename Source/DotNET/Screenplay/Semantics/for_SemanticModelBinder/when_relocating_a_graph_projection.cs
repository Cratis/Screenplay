// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_relocating_a_graph_projection : given.a_graph_projection_source
{
    ExecutableSemanticModel _before;
    ExecutableSemanticModel _after;

    void Establish() => _before = Compile(Source);

    void Because() => _after = Compile(Source.Replace(Projection, string.Empty) + "\n  feature OtherChart\n    slice StateView Details\n" + Projection + "\n");

    [Fact] void should_change_the_declaring_slice() => _after.Application.GraphProjections.Single().DeclaringSlice.ShouldNotEqual(_before.Application.GraphProjections.Single().DeclaringSlice);
    [Fact] void should_change_the_semantic_revision() => _after.Revision.ShouldNotEqual(_before.Revision);
}

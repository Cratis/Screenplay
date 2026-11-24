// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class reversed_graph_projection_identities : given.graph_projection_models
{
    ExecutableSemanticModel _provisional;
    ExecutableSemanticModel _assigned;

    void Establish()
    {
        _provisional = Compile(GraphSource);
        _assigned = Compile(GraphSource, Reversed(Workspace(GraphSource).IdentityCatalog));
    }

    void Because() => _comparison = SemanticModelEquivalence.Compare(_provisional, _assigned);

    [Fact] void should_assign_a_different_declaring_identity() => _assigned.Application.GraphProjections.Single().DeclaringSlice.ShouldNotEqual(_provisional.Application.GraphProjections.Single().DeclaringSlice);
    [Fact] void should_be_equivalent() => _comparison.IsEquivalent.ShouldBeTrue();
}

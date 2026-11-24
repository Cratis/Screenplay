// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class graph_projections_in_different_slices : given.graph_projection_models
{
    ExecutableSemanticModel _original;
    ExecutableSemanticModel _moved;

    void Establish()
    {
        _original = Compile(GraphSource);
        var feature = _original.Application.Modules.Single().Features.Single();
        _moved = ExecutableSemanticModel.Create(LanguageVersion.V58, SemanticVersion.V58, _original.Application with
        {
            GraphProjections = [_original.Application.GraphProjections.Single() with { DeclaringSlice = feature.Slices.Single(slice => slice.Name == "Details").Id }]
        });
    }

    void Because() => _comparison = SemanticModelEquivalence.Compare(_original, _moved);

    [Fact] void should_not_be_equivalent() => _comparison.IsEquivalent.ShouldBeFalse();
}

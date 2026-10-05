// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_the_v6_golden_model : Specification
{
    byte[] _expected = [];
    byte[] _written = [];
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _expected = canonical_serialization_golden_vectors.SemanticModelV6Bytes;
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV6();
    }

    void Because() => _written = SemanticModelSerializer.Serialize(_model);

    [Fact] void should_match_the_checked_in_v6_bytes() => _written.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_preserve_the_v6_revision() => SemanticModelSerializer.Deserialize(_expected).Revision.ShouldEqual(_model.Revision);
    [Fact] void should_retain_v5_absence() => _model.Application.Modules.Single().Features.SelectMany(AllSlices).SelectMany(_ => _.Specifications).Any(_ => !_.ThenAbsentReadModels.IsEmpty).ShouldBeTrue();
    [Fact] void should_hold_reactions_and_captures() => _model.Application.Modules.Single().Features.Single(_ => _.Name == "Automation").Slices.Sum(_ => _.Reactions.Length + _.Captures.Length).ShouldEqual(4);

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(AllSlices));
}
#endif

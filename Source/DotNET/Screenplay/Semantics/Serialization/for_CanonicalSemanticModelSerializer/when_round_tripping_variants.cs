// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_variants : Specification
{
    byte[] _bytes;
    SemanticProjection[] _variants;

    void Because()
    {
        _bytes = SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModel());
        var model = SemanticModelSerializer.Deserialize(_bytes);
        _variants = [.. model.Application.Modules.Single().Features.Single().Features.Single().Slices
            .Single(slice => slice.Name == "Variants").Projections];
    }

    [Fact] void should_retain_two_independent_projections() => _variants.Length.ShouldEqual(2);
    [Fact] void should_preserve_the_update_only_join_key() => _variants.All(variant => variant.Scope!.Joins.Single().Key is not null).ShouldBeTrue();
    [Fact] void should_preserve_mutual_exclusion() => _variants.All(variant => variant.Scope!.Removals.Single().Key == SemanticProjectionKey.EventSourceIdentity).ShouldBeTrue();
    [Fact] void should_round_trip_identically() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).SequenceEqual(_bytes).ShouldBeTrue();
}

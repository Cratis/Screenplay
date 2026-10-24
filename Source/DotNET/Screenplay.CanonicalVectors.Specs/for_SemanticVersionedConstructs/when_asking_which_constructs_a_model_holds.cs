// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticVersionedConstructs;

public class when_asking_which_constructs_a_model_holds : Specification
{
    SemanticApplication _dates;
    SemanticApplication _composite;
    SemanticApplication _uuids;
    SemanticApplication _plain;

    void Because()
    {
        _dates = DateComparisonsCorpus.V26.Model();
        _composite = CompositeReadModelKeysCorpus.V27.Model();
        _uuids = UuidComparisonsCorpus.V28.Model();
        _plain = ProtectedCommandReadsCorpus.V25.Model();
    }

    [Fact] void should_see_date_comparisons() => SemanticVersionedConstructs.UsesDateComparisons(_dates).ShouldBeTrue();
    [Fact] void should_see_composite_keys() => SemanticVersionedConstructs.UsesCompositeReadModelKeys(_composite).ShouldBeTrue();
    [Fact] void should_see_uuid_comparisons() => SemanticVersionedConstructs.UsesUuidComparisons(_uuids).ShouldBeTrue();
    [Fact] void should_not_see_date_comparisons_elsewhere() => SemanticVersionedConstructs.UsesDateComparisons(_plain).ShouldBeFalse();
    [Fact] void should_not_see_composite_keys_elsewhere() => SemanticVersionedConstructs.UsesCompositeReadModelKeys(_plain).ShouldBeFalse();
    [Fact] void should_not_see_uuid_comparisons_elsewhere() => SemanticVersionedConstructs.UsesUuidComparisons(_plain).ShouldBeFalse();
}

static class VectorModels
{
    public static SemanticApplication Model(this CanonicalCorpusVector vector) => SemanticModelSerializer.Deserialize(vector.EsmBytes.AsSpan()).Application;
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v25_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _read;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV25());
        _read = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_pin_the_new_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV25Bytes);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_read).ShouldEqual(_bytes);
    [Fact] void should_select_the_allocated_version() => _read.SemanticVersion.ShouldEqual(SemanticVersion.V25);
    [Fact] void should_keep_v23_support_frozen() => EsmSchemaV23Support.Supports(LanguageVersion.V25, SemanticVersion.V25).ShouldBeFalse();
}

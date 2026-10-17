// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v24_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _read;

    void Because()
    {
        _bytes = canonical_serialization_golden_vectors.EsmV24Bytes;
        _read = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_preserve_the_language_version() => _read.LanguageVersion.ShouldEqual(LanguageVersion.V24);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_read).ShouldEqual(_bytes);
    [Fact] void should_select_the_allocated_version() => _read.SemanticVersion.ShouldEqual(SemanticVersion.V24);
    [Fact] void should_keep_v23_support_frozen() => EsmSchemaV23Support.Supports(LanguageVersion.V24, SemanticVersion.V24).ShouldBeFalse();
}

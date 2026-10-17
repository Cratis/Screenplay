// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v19_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _read;

    void Because()
    {
        _bytes = canonical_serialization_golden_vectors.EsmV19Bytes;
        _read = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_preserve_the_language_version() => _read.LanguageVersion.ShouldEqual(LanguageVersion.V19);
    [Fact] void should_round_trip_without_byte_changes() => SemanticModelSerializer.Serialize(_read).ShouldEqual(_bytes);
    [Fact] void should_use_v19() => _read.SemanticVersion.ShouldEqual(SemanticVersion.V19);
    [Fact] void should_refuse_branches_below_v19() => Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(_bytes).Replace("\"schemaVersion\":19", "\"schemaVersion\":17", StringComparison.Ordinal).Replace("19.0", "17.0", StringComparison.Ordinal)))).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_programmatic_branches_below_v19() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V17, SemanticVersion.V17, _read.Application)).ShouldBeOfExactType<InvalidSemanticContract>();
}

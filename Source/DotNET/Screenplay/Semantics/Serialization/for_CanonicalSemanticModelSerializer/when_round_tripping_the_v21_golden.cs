// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v21_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _read;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV21());
        _read = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_pin_only_the_new_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV21Bytes);
    [Fact] void should_round_trip() => SemanticModelSerializer.Serialize(_read).ShouldEqual(_bytes);
    [Fact] void should_select_v21() => _read.SemanticVersion.ShouldEqual(SemanticVersion.V21);
    [Fact] void should_keep_the_frozen_v20_support_closed() => EsmSchemaV20Support.Supports(LanguageVersion.V21, SemanticVersion.V21).ShouldBeFalse();
    [Fact] void should_refuse_a_pre_v21_model() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V20, SemanticVersion.V20, _read.Application)).ShouldBeOfExactType<InvalidSemanticContract>();

    [Theory]
    [InlineData("\"min\":-9223372036854775808", "\"min\":-9223372036854775809")]
    [InlineData("\"max\":9223372036854775807", "\"max\":9223372036854775808")]
    [InlineData("\"min\":-9223372036854775808", "\"min\":1.0")]
    [InlineData("\"min\":-9223372036854775808", "\"min\":null")]
    [InlineData("\"pattern\":\"[AB]{1,2}\"", "\"pattern\":null")]
    [InlineData("\"checksum\":\"luhn\"", "\"checksum\":\"unknown\"")]
    [InlineData("\"kind\":\"random\"", "\"kind\":\"random\",\"checksum\":\"luhn\"")]
    [InlineData("\"kind\":\"pattern\"", "\"kind\":\"pattern\",\"kind\":\"pattern\"")]
    void should_refuse_invalid_wire_members(string original, string replacement)
    {
        var json = Encoding.UTF8.GetString(_bytes);
        json.Contains(original, StringComparison.Ordinal).ShouldBeTrue();
        Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json.Replace(original, replacement, StringComparison.Ordinal)))).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}

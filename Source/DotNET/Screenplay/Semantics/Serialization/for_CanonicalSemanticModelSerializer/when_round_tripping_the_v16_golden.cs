// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v16_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV16();
        _bytes = SemanticModelSerializer.Serialize(_model);
    }

    [Fact] void should_pin_bytes() => _bytes.SequenceEqual(canonical_serialization_golden_vectors.EsmV16Bytes).ShouldBeTrue();
    [Fact] void should_round_trip_bytes_and_revision() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).SequenceEqual(_bytes).ShouldBeTrue();

    [Theory]
    [InlineData("\"thenNoEvents\":true", "\"thenNoEvents\":false")]
    [InlineData("\"given\":0", "\"given\":-1")]
    [InlineData("\"given\":0", "\"given\":99")]
    [InlineData("\"given\":0", "\"given\":0,\"extra\":true")]
    void should_refuse_invalid_canonical_members(string from, string to) => Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(_bytes).Replace(from, to, StringComparison.Ordinal)))).ShouldBeOfExactType<InvalidSemanticContract>();

    [Fact]
    void should_refuse_members_in_a_prior_schema()
    {
        var json = Encoding.UTF8.GetString(_bytes).Replace("\"schemaVersion\":16", "\"schemaVersion\":15", StringComparison.Ordinal)
            .Replace("\"languageVersion\":\"16.0\"", "\"languageVersion\":\"15.0\"", StringComparison.Ordinal)
            .Replace("\"semanticVersion\":\"16.0\"", "\"semanticVersion\":\"15.0\"", StringComparison.Ordinal);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("whenRedelivered");
    }
}

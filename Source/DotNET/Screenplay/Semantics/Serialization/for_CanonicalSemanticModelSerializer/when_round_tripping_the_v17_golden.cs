// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v17_golden : Specification
{
    byte[] _bytes;

    void Because()
    {
        _bytes = feature_corpus_vectors.Read("RecoverySpecifications/v17/expected/esm-v17.json");
    }

    [Fact] void should_round_trip_bytes_and_revision() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).SequenceEqual(_bytes).ShouldBeTrue();

    [Theory]
    [InlineData("\"thenNoEvents\":true", "\"thenNoEvents\":false")]
    [InlineData("\"given\":0", "\"given\":-1")]
    [InlineData("\"given\":0", "\"given\":99")]
    [InlineData("\"given\":0", "\"given\":0,\"extra\":true")]
    void should_refuse_invalid_canonical_members(string from, string to)
    {
        var json = Encoding.UTF8.GetString(_bytes);
        json.Contains(from, StringComparison.Ordinal).ShouldBeTrue();
        Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json.Replace(from, to, StringComparison.Ordinal)))).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_refuse_members_in_a_prior_schema()
    {
        var json = Encoding.UTF8.GetString(_bytes).Replace("\"schemaVersion\":17", "\"schemaVersion\":16", StringComparison.Ordinal)
            .Replace("\"languageVersion\":\"17.0\"", "\"languageVersion\":\"16.0\"", StringComparison.Ordinal)
            .Replace("\"semanticVersion\":\"17.0\"", "\"semanticVersion\":\"16.0\"", StringComparison.Ordinal);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("whenRedelivered");
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_reaction_identity : Specification
{
    [Fact]
    void should_preserve_the_v10_golden_and_empty_roles()
    {
        var bytes = SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV10());
        bytes.SequenceEqual(canonical_serialization_golden_vectors.EsmV10Bytes).ShouldBeTrue();
        SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(bytes)).SequenceEqual(bytes).ShouldBeTrue();
        Encoding.UTF8.GetString(bytes).ShouldContain("\"runsAs\":{\"kind\":\"system\",\"roles\":[]}");
    }

    [Theory]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(9)]
    void should_reject_runs_as_under_earlier_schemas(int schema)
    {
        var json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV6()));
        json = json.Replace("\"schemaVersion\":6", $"\"schemaVersion\":{schema}", StringComparison.Ordinal)
            .Replace("\"languageVersion\":\"6.0\"", $"\"languageVersion\":\"{schema}.0\"", StringComparison.Ordinal)
            .Replace("\"semanticVersion\":\"6.0\"", $"\"semanticVersion\":\"{schema}.0\"", StringComparison.Ordinal)
            .Replace("\"name\":\"BatchHandler\",\"triggers\":", "\"name\":\"BatchHandler\",\"runsAs\":{\"kind\":\"system\",\"roles\":[]},\"triggers\":", StringComparison.Ordinal);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("runsAs");
    }

    [Fact]
    void should_reject_a_non_system_kind()
    {
        var json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV10()))
            .Replace("\"kind\":\"system\"", "\"kind\":\"person\"", StringComparison.Ordinal);
        Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json))).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v18_golden : Specification
{
    byte[] _bytes;

    void Because()
    {
        _bytes = canonical_serialization_golden_vectors.EsmV18Bytes;
    }

    [Fact] void should_select_v18() => SemanticModelSerializer.Deserialize(_bytes).SemanticVersion.ShouldEqual(SemanticVersion.V18);
    [Fact] void should_preserve_the_language_version() => SemanticModelSerializer.Deserialize(_bytes).LanguageVersion.ShouldEqual(LanguageVersion.V18);
    [Fact] void should_round_trip_bytes_and_revision() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);

    [Fact]
    void should_refuse_query_rejections_in_a_prior_schema()
    {
        var json = Encoding.UTF8.GetString(_bytes).Replace("\"schemaVersion\":18", "\"schemaVersion\":17", StringComparison.Ordinal)
            .Replace("\"languageVersion\":\"18.0\"", "\"languageVersion\":\"17.0\"", StringComparison.Ordinal)
            .Replace("\"semanticVersion\":\"18.0\"", "\"semanticVersion\":\"17.0\"", StringComparison.Ordinal);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error.Message.ShouldContain("Query validation rejection specifications require ESM v18");
    }
}

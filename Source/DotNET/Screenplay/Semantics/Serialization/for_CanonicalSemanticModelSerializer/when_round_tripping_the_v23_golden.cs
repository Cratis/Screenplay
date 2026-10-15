// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v23_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _read;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _bytes = SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV23());
        _read = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_pin_the_new_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV23Bytes);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_read).ShouldEqual(_bytes);
    [Fact] void should_select_v23() => _read.SemanticVersion.ShouldEqual(SemanticVersion.V23);
    [Fact] void should_keep_v22_support_frozen() => EsmSchemaV22Support.Supports(LanguageVersion.V23, SemanticVersion.V23).ShouldBeFalse();
    [Theory]
    [InlineData("\"handler\":{", "\"handler\":null,\"ignored\":{")]
    [InlineData("\"resultVersion\":1", "\"resultVersion\":2")]
    [InlineData("\"resultVersion\":1", "\"resultVersion\":1,\"extra\":true")]
    [InlineData("\"resultVersion\":1", "\"resultVersion\":1,\"resultVersion\":1")]
    [InlineData(",\"resultVersion\":1", "")]
    [InlineData("\"schemaVersion\":23", "\"schemaVersion\":22")]
    void should_refuse_invalid_wire_handlers(string original, string replacement)
    {
        var json = Encoding.UTF8.GetString(_bytes);
        json.Contains(original, StringComparison.Ordinal).ShouldBeTrue();
        Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json.Replace(original, replacement, StringComparison.Ordinal)))).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v22_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _read;

    void Because()
    {
        _bytes = canonical_serialization_golden_vectors.EsmV22Bytes;
        _read = SemanticModelSerializer.Deserialize(_bytes);
    }

    [Fact] void should_preserve_the_language_version() => _read.LanguageVersion.ShouldEqual(LanguageVersion.V22);
    [Fact] void should_round_trip_strictly() => SemanticModelSerializer.Serialize(_read).ShouldEqual(_bytes);
    [Fact] void should_select_v22() => _read.SemanticVersion.ShouldEqual(SemanticVersion.V22);
    [Fact] void should_select_atomicity_explicitly() => _read.Application.ReactionProductionMode.ShouldEqual(SemanticReactionProductionMode.Atomic);
    [Theory]
    [InlineData("\"atomic\"", "null")]
    [InlineData("\"atomic\"", "false")]
    [InlineData("\"atomic\"", "\"legacy\"")]
    [InlineData("\"reactionProductionMode\":\"atomic\"", "\"reactionProductionMode\":\"atomic\",\"reactionProductionMode\":\"atomic\"")]
    [InlineData("\"schemaVersion\":22", "\"schemaVersion\":21")]
    void should_refuse_invalid_wire_modes(string original, string replacement)
    {
        var json = Encoding.UTF8.GetString(_bytes);
        json.Contains(original, StringComparison.Ordinal).ShouldBeTrue();
        Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json.Replace(original, replacement, StringComparison.Ordinal)))).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact] void should_keep_v21_support_frozen() => EsmSchemaV21Support.Supports(LanguageVersion.V22, SemanticVersion.V22).ShouldBeFalse();
}

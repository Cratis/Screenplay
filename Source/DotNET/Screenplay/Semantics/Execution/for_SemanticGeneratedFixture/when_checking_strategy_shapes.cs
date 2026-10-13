// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticGeneratedFixture;

public class when_checking_strategy_shapes : Specification
{
    [Theory]
    [InlineData("uuid", "00112233-4455-4677-8899-aabbccddeeff", true)]
    [InlineData("uuid", "00112233-4455-7677-8899-aabbccddeeff", false)]
    [InlineData("uuidv7", "00112233-4455-7677-8899-aabbccddeeff", true)]
    [InlineData("uuidv7", "00112233-4455-7677-7899-aabbccddeeff", false)]
    [InlineData("ulid", "01ARZ3NDEKTSV4RRFFQ69G5FAV", true)]
    [InlineData("ulid", "81ARZ3NDEKTSV4RRFFQ69G5FAV", false)]
    [InlineData("ulid", "01arz3ndektsv4rrffq69g5fav", false)]
    [InlineData("typeid", "0000000000e00800000000000\u017F", false)]
    [InlineData("typeid", "0000000000e008000000000000", true)]
    [InlineData("typeid", "_0000000000e008000000000000", false)]
    [InlineData("typeid", "00000000008008000000000000", false)]
    [InlineData("nanoid", "Ab_-", true)]
    [InlineData("nanoid", "Ab=+", false)]
    void should_check_builtin_shapes(string kind, string value, bool expected)
    {
        var generation = new SemanticGeneration(kind) { Prefix = kind == "typeid" ? "" : null, Size = kind == "nanoid" ? 4 : null };
        var concept = new SemanticConcept(default, "Value", SemanticPrimitiveType.Text, [], []) { Generation = generation };
        SemanticGeneratedFixture.Matches(concept, SemanticValue.Text(value)).ShouldEqual(expected);
    }

    [Theory]
    [InlineData("uuid", "upper", null, null, "ref_00112233-4455-4677-8899-AABBCCDDEEFF", true)]
    [InlineData("uuid", "upper", null, null, "REF_00112233-4455-4677-8899-AABBCCDDEEFF", false)]
    [InlineData("uuidv7", "lower", null, null, "ref_00112233-4455-7677-8899-aabbccddeeff", true)]
    [InlineData("ulid", "lower", null, null, "ref_01arz3ndektsv4rrffq69g5fav", true)]
    [InlineData("ulid", "lower", null, null, "ref_01ARZ3NDEKTSV4RRFFQ69G5FAV", false)]
    [InlineData("ulid", "lower", null, null, "ref_01arz3ndektsv4rrffq69g5fa\u017F", false)]
    [InlineData("nanoid", null, null, "ab", "ref_abab", true)]
    [InlineData("nanoid", null, null, "ab", "ref_abac", false)]
    [InlineData("bytes", "upper", "hex", null, "ref_FF", true)]
    [InlineData("bytes", "upper", "hex", null, "ref_ff", false)]
    [InlineData("bytes", "lower", "base32", null, "ref_74", true)]
    [InlineData("bytes", "lower", "base32", null, "ref_ab", false)]
    void should_check_declared_modifiers(string kind, string? letterCase, string? encoding, string? alphabet, string value, bool expected)
    {
        var generation = new SemanticGeneration(kind) { Prefix = "ref_", Case = letterCase, Encoding = encoding, Alphabet = alphabet, Count = kind == "bytes" ? 1 : null, Size = kind == "nanoid" ? 4 : null };
        var concept = new SemanticConcept(default, "Value", SemanticPrimitiveType.Text, [], []) { Generation = generation };
        SemanticGeneratedFixture.Matches(concept, SemanticValue.Text(value)).ShouldEqual(expected);
    }

    [Theory]
    [InlineData("base64url", "AA", true)]
    [InlineData("base64url", "_w", true)]
    [InlineData("base64url", "AB", false)]
    [InlineData("base64url", "_x", false)]
    [InlineData("base64url", "AA==", false)]
    [InlineData("base32", "AA", true)]
    [InlineData("base32", "74", true)]
    [InlineData("base32", "AB", false)]
    [InlineData("base32", "aa", false)]
    [InlineData("hex", "ff", true)]
    [InlineData("hex", "FF", false)]
    [InlineData("hex", "fff", false)]
    void should_require_exact_byte_count_and_zero_trailing_bits(string encoding, string value, bool expected)
    {
        var concept = new SemanticConcept(default, "Value", SemanticPrimitiveType.Text, [], []) { Generation = new("bytes") { Count = 1, Encoding = encoding } };
        SemanticGeneratedFixture.Matches(concept, SemanticValue.Text(value)).ShouldEqual(expected);
    }
}

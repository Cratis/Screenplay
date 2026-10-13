// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_GenerationEntropy;

public class when_calculating_entropy
{
    [Theory]
    [InlineData("uuid", 122)]
    [InlineData("uuidv7", 74)]
    [InlineData("typeid", 74)]
    [InlineData("ulid", 80)]
    [InlineData("nanoid", 126)]
    void should_calculate_builtin_defaults(string strategy, double bits) => GenerationEntropy.Bits(new(strategy, SourceLocation.Start)).ShouldEqual(bits);

    [Theory]
    [InlineData(32, "ab", 32)]
    [InlineData(8, "0123456789abcdef", 32)]
    [InlineData(13, "ABC", 20.604512509375028)]
    void should_count_only_random_alphabet_positions(int size, string alphabet, double bits) =>
        GenerationEntropy.Bits(new("nanoid", SourceLocation.Start) { Size = size, Alphabet = alphabet, Prefix = "reference_" }).ShouldEqual(bits);

    [Theory]
    [InlineData(8, "hex", 64)]
    [InlineData(32, "base64url", 256)]
    [InlineData(512, "base32", 4096)]
    void should_count_random_bytes_not_encoded_characters(int count, string encoding, double bits) =>
        GenerationEntropy.Bits(new("bytes", SourceLocation.Start) { Count = count, Encoding = encoding }).ShouldEqual(bits);

    [Theory]
    [InlineData("pattern")]
    [InlineData("random")]
    [InlineData("rule")]
    [InlineData("sequence")]
    void should_not_estimate_preview_entropy(string strategy) => GenerationEntropy.Bits(new(strategy, SourceLocation.Start)).ShouldBeNull();

    [Theory]
    [InlineData(1, "")]
    [InlineData(1, "a")]
    [InlineData(1, "aa")]
    [InlineData(0, "ab")]
    [InlineData(-1, "ab")]
    void should_not_estimate_entropy_for_invalid_nanoid_inputs(int size, string alphabet) =>
        GenerationEntropy.Bits(new("nanoid", SourceLocation.Start) { Size = size, Alphabet = alphabet }).ShouldBeNull();

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    void should_not_estimate_entropy_without_positive_byte_counts(int? count) =>
        GenerationEntropy.Bits(new("bytes", SourceLocation.Start) { Count = count }).ShouldBeNull();

    [Fact] void should_avoid_integer_overflow_for_byte_entropy() => GenerationEntropy.Bits(new("bytes", SourceLocation.Start) { Count = int.MaxValue }).ShouldEqual(8d * int.MaxValue);
    [Fact] void should_share_the_semantic_contract_entropy() => SemanticGenerationContract.Bits(new("nanoid") { Size = 13, Alphabet = "ABC" }).ShouldEqual(GenerationEntropy.Bits(new("nanoid", SourceLocation.Start) { Size = 13, Alphabet = "ABC" })!.Value);
    [Fact] void should_use_implicit_uuid_entropy() => GenerationEntropy.Bits(null).ShouldEqual(122);
    [Fact] void should_preserve_the_binders_collision_estimate() => GenerationEntropy.ValuesAtOnePercentCollision(32).ShouldEqual(Math.Sqrt(2 * Math.Pow(2, 32) * Math.Log(1 / 0.99)));
    [Fact] void should_format_large_counts_without_infinity() => GenerationEntropy.CollisionCountText(4096).ShouldContain("e615");
}

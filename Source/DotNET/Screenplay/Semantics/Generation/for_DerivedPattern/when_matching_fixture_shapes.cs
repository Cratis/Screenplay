// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.Generation.for_DerivedPattern;

public class when_matching_fixture_shapes
{
    [Theory]
    [InlineData("uuid", "lower")]
    [InlineData("uuid", "upper")]
    [InlineData("uuidv7", "lower")]
    [InlineData("uuidv7", "upper")]
    [InlineData("ulid", "lower")]
    [InlineData("ulid", "upper")]
    [InlineData("typeid", null)]
    [InlineData("nanoid", null)]
    void should_agree_with_the_existing_fixture_validator_at_every_position(string kind, string? letterCase) =>
        Check(new(kind) { Prefix = "id", Case = letterCase, Size = kind == "nanoid" ? 5 : null });

    [Theory]
    [InlineData("base64url", null)]
    [InlineData("hex", "upper")]
    [InlineData("hex", "lower")]
    [InlineData("base32", "upper")]
    [InlineData("base32", "lower")]
    void should_preserve_zero_unused_bits_for_every_byte_residue(string encoding, string? letterCase)
    {
        for (var count = 1; count <= 10; count++) Check(new("bytes") { Count = count, Encoding = encoding, Case = letterCase, Prefix = "key_" });
    }

    static void Check(SemanticGeneration generation)
    {
        var concept = new SemanticConcept(default, "Id", SemanticPrimitiveType.Text, [], []) { Generation = generation };
        var shape = GenerationShape.Create(generation)!;
        var sample = shape.Samples().First();
        var pattern = DerivedPattern.Create(generation)!;
        var regex = SemanticMatchPattern.Create(pattern);
        RuleConsistency.Check(generation, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, new SemanticTextValue(pattern), null)).Status.ShouldEqual(RuleConsistencyStatus.Proven);
        foreach (var value in shape.Samples()) SemanticGeneratedFixture.Matches(concept, new SemanticTextValue(value)).ShouldBeTrue();
        for (var index = 0; index < sample.Length; index++)
        {
            foreach (var character in RegexCharacters.Printable)
            {
                var value = sample[..index] + character + sample[(index + 1)..];
                regex.IsMatch(value).ShouldEqual(SemanticGeneratedFixture.Matches(concept, new SemanticTextValue(value)));
            }
        }
    }
}

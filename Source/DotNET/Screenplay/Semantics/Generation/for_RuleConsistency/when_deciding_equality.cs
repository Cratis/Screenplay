// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_deciding_equality
{
    [Theory]
    [InlineData("aa", "ab")]
    [InlineData("ab", "aa")]
    [InlineData("outside", "aa")]
    [InlineData("é", "aa")]
    void should_refute_equal_with_the_least_other_value(string operand, string witness) =>
        RuleConsistency.Check(new("nanoid") { Size = 2, Alphabet = "ba" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Equal, new SemanticTextValue(operand), null)).Witness.ShouldEqual(witness);

    [Theory]
    [InlineData("aa")]
    [InlineData("bb")]
    void should_refute_not_equal_with_the_operand_itself(string operand) =>
        RuleConsistency.Check(new("nanoid") { Size = 2, Alphabet = "ba" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.NotEqual, new SemanticTextValue(operand), null)).Witness.ShouldEqual(operand);

    [Theory]
    [InlineData("é")]
    [InlineData("ab\n")]
    [InlineData("a")]
    [InlineData("cc")]
    void should_prove_not_equal_to_a_value_outside_the_shape(string operand) =>
        RuleConsistency.Check(new("nanoid") { Size = 2, Alphabet = "ba" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.NotEqual, new SemanticTextValue(operand), null)).Status.ShouldEqual(RuleConsistencyStatus.Proven);

    [Fact] void should_treat_regex_punctuation_as_literal_equality() => RuleConsistency.Check(new("nanoid") { Size = 1, Alphabet = ".*" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.NotEqual, new SemanticTextValue("."), null)).Witness.ShouldEqual(".");
}

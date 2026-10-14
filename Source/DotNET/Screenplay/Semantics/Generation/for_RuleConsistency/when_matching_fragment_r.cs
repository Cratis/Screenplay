// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_matching_fragment_r
{
    [Theory]
    [InlineData("^(?:a|b){4}$")]
    [InlineData("^[ab]{2,}$")]
    [InlineData("^[ab]{2,8}?$")]
    [InlineData("^[ab]+?$")]
    [InlineData("^[^c]+$")]
    [InlineData("^\\w+$")]
    [InlineData("^\\D+$")]
    [InlineData("^\\S+$")]
    [InlineData("^....$")]
    [InlineData("^(a?b?){1000000}$")]
    [InlineData("^[ab]{0}$|^[ab]{4}$")]
    void should_prove_supported_languages(string pattern) => Check(pattern).Status.ShouldEqual(RuleConsistencyStatus.Proven);

    [Theory]
    [InlineData("^a+$", "aaab")]
    [InlineData("a", "bbbb")]
    [InlineData("^b", "aaaa")]
    [InlineData("b$", "aaaa")]
    [InlineData("^a|b$", "baaa")]
    [InlineData("^\\d+$", "aaaa")]
    [InlineData("^[ab]{5}$", "aaaa")]
    [InlineData("^[ab]{999999}$", "aaaa")]
    [InlineData("^a{0}b{4}$", "aaaa")]
    [InlineData("^a{1,3}b?$", "aaaa")]
    void should_return_the_ordinal_least_violation(string pattern, string witness) => Check(pattern).Witness.ShouldEqual(witness);

    [Fact] void should_prove_the_portable_email_rule() => RuleConsistency.Check(new("nanoid") { Size = 2, Alphabet = "ab", Prefix = "u@shop." }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, new SemanticTextValue(SemanticMatchPattern.Email), null)).Status.ShouldEqual(RuleConsistencyStatus.Proven);
    [Fact] void should_accept_an_empty_unanchored_pattern() => Check(string.Empty).Status.ShouldEqual(RuleConsistencyStatus.Proven);
    [Fact] void should_refute_an_anchored_empty_pattern() => Check("^$").Witness.ShouldEqual("aaaa");
    [Fact] void should_project_space_only_for_whitespace() => RuleConsistency.Check(new("nanoid") { Size = 2, Alphabet = " a" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, new SemanticTextValue("^\\s+$"), null)).Witness.ShouldEqual(" a");

    static RuleConsistencyResult Check(string pattern) => RuleConsistency.Check(new("nanoid") { Size = 4, Alphabet = "ba" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, new SemanticTextValue(pattern), null));
}

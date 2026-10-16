// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_sampling_outside_fragment_r
{
    [Theory]
    [InlineData("(?<word>[ab]+)", "named group")]
    [InlineData("^[\\x61b]+$", "\\x escape")]
    [InlineData("^[\\u0061b]+$", "\\u escape")]
    [InlineData("[ab]+|é", "non-ASCII")]
    [InlineData("[ab]+|a^", "interior anchor")]
    [InlineData("(^[ab]+$)", "interior anchor")]
    void should_not_prove_from_passing_samples(string pattern, string construct) => Check(pattern).ShouldEqual(RuleConsistencyResult.Unproven("outside fragment R: " + construct));

    [Theory]
    [InlineData("(?=a|b)[ab]+", "lookaround")]
    [InlineData("\\b[ab]+\\b", "word boundary")]
    [InlineData("(a|b)\\1|ab", "backreference")]
    [InlineData("(?i)[ab]+", "inline options")]
    [InlineData("[ab]+|[]", "empty character class")]
    [InlineData("[^]", "negated empty character class")]
    [InlineData("[ab]+|}", "unescaped brace")]
    [InlineData("[ab]+|]", "unescaped closing bracket")]
    [InlineData("[ab]+|{", "unescaped brace")]
    [InlineData("^(?=b)[ab]+$", "lookaround")]
    [InlineData("^(?=a)[ab]+$", "lookaround")]
    [InlineData("^(?=aa|bb)[ab]+$", "lookaround")]
    void should_classify_refused_patterns_without_sampling(string pattern, string construct) => Check(pattern).ShouldEqual(RuleConsistencyResult.Unproven("outside the portable match dialect: " + construct));

    [Fact] void should_report_an_invalid_runtime_regex_as_unproven() => Check("[").Reason.ShouldEqual("outside the portable match dialect: unterminated character class");

    [Theory]
    [InlineData(SemanticValidationRuleKind.RulePredicate)]
    [InlineData(SemanticValidationRuleKind.CodeValidation)]
    void should_report_opaque_rules(SemanticValidationRuleKind kind) => RuleConsistency.Check(new("uuid"), SemanticPrimitiveType.Uuid, new(default, kind, null, null)).Reason.ShouldEqual("opaque");

    static RuleConsistencyResult Check(string pattern) => RuleConsistency.Check(new("nanoid") { Size = 2, Alphabet = "ba" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, new SemanticTextValue(pattern), null));
}

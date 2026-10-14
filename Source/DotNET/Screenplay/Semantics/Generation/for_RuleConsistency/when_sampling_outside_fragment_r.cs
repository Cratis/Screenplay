// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_sampling_outside_fragment_r
{
    [Theory]
    [InlineData("(?=a|b)[ab]+", "lookaround")]
    [InlineData("\\b[ab]+\\b", "word boundary")]
    [InlineData("(a|b)\\1|ab", "backreference")]
    [InlineData("(?<word>[ab]+)", "named group")]
    [InlineData("(?i)[ab]+", "flags or group extension")]
    [InlineData("^[\\x61b]+$", "\\x escape")]
    [InlineData("^[\\u0061b]+$", "\\u escape")]
    [InlineData("[ab]+|é", "non-ASCII")]
    [InlineData("[ab]+|[]", "empty character class")]
    [InlineData("[^]", "empty character class")]
    [InlineData("[ab]+|}", "unescaped }")]
    [InlineData("[ab]+|]", "unescaped ]")]
    [InlineData("[ab]+|{", "unescaped {")]
    [InlineData("[ab]+|a^", "interior anchor")]
    [InlineData("(^[ab]+$)", "interior anchor")]
    void should_not_prove_from_passing_samples(string pattern, string construct) => Check(pattern).ShouldEqual(RuleConsistencyResult.Unproven("outside fragment R: " + construct));

    [Fact] void should_refute_with_the_first_failing_minimum_sample() => Check("^(?=b)[ab]+$").Witness.ShouldEqual("aa");
    [Fact] void should_refute_with_the_maximum_sample_after_minimum_passes() => Check("^(?=a)[ab]+$").Witness.ShouldEqual("bb");
    [Fact] void should_refute_with_the_alternating_sample_after_extremes_pass() => Check("^(?=aa|bb)[ab]+$").Witness.ShouldEqual("ab");
    [Fact] void should_report_an_invalid_runtime_regex_as_unproven() => Check("[").Reason.ShouldEqual("outside fragment R: empty character class");

    [Theory]
    [InlineData(SemanticValidationRuleKind.RulePredicate)]
    [InlineData(SemanticValidationRuleKind.CodeValidation)]
    void should_report_opaque_rules(SemanticValidationRuleKind kind) => RuleConsistency.Check(new("uuid"), SemanticPrimitiveType.Uuid, new(default, kind, null, null)).Reason.ShouldEqual("opaque");

    static RuleConsistencyResult Check(string pattern) => RuleConsistency.Check(new("nanoid") { Size = 2, Alphabet = "ba" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, new SemanticTextValue(pattern), null));
}

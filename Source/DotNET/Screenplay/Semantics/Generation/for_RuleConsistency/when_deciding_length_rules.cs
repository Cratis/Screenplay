// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_deciding_length_rules
{
    [Theory]
    [InlineData(SemanticValidationRuleKind.Minimum, 3, RuleConsistencyStatus.Proven)]
    [InlineData(SemanticValidationRuleKind.Minimum, 4, RuleConsistencyStatus.Refuted)]
    [InlineData(SemanticValidationRuleKind.Maximum, 3, RuleConsistencyStatus.Proven)]
    [InlineData(SemanticValidationRuleKind.Maximum, 2, RuleConsistencyStatus.Refuted)]
    [InlineData(SemanticValidationRuleKind.Length, 3, RuleConsistencyStatus.Proven)]
    [InlineData(SemanticValidationRuleKind.Length, 4, RuleConsistencyStatus.Refuted)]
    internal void should_decide_fixed_text_lengths(SemanticValidationRuleKind kind, int operand, RuleConsistencyStatus status) =>
        RuleConsistency.Check(new("nanoid") { Size = 2, Alphabet = "ba", Prefix = "X" }, SemanticPrimitiveType.Text, new(default, kind, new SemanticNumberValue(operand), null)).Status.ShouldEqual(status);

    [Fact] void should_report_the_ordinal_minimum_on_refuted_length() => RuleConsistency.Check(new("nanoid") { Size = 2, Alphabet = "ba", Prefix = "X" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Length, new SemanticNumberValue(4), null)).Witness.ShouldEqual("Xaa");
    [Fact] void should_admit_whitespace_as_nonempty_under_current_semantics() => RuleConsistency.Check(new("nanoid") { Size = 1, Alphabet = " a" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.NotEmpty, null, null)).Status.ShouldEqual(RuleConsistencyStatus.Proven);
    [Fact] void should_not_apply_the_pattern_position_limit_to_builtin_shapes() => RuleConsistency.Check(new("bytes") { Count = 512, Encoding = "hex", Prefix = "key_" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Length, new SemanticNumberValue(1028), null)).Status.ShouldEqual(RuleConsistencyStatus.Proven);
    [Fact] void should_not_depend_on_severity() => RuleConsistency.Check(new("nanoid") { Size = 1, Alphabet = "ba" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Length, new SemanticNumberValue(2), null) { Severity = SemanticValidationSeverity.Information }).Status.ShouldEqual(RuleConsistencyStatus.Refuted);
}

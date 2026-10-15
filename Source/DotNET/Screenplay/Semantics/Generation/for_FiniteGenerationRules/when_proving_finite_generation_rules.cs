// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_FiniteGenerationRules;

public class when_proving_finite_generation_rules
{
    [Fact]
    void should_prove_variable_length_containment_without_counting_decompositions() => RuleConsistency.CheckPattern("[AB]{1,2}[AB]{1,2}", null, Match("^[AB]+$")).Status.ShouldEqual(RuleConsistencyStatus.Proven);

    [Fact]
    void should_keep_ambiguous_optional_repeat_unrolling_unproven()
    {
        var result = RuleConsistency.CheckPattern("[AB]{1,2}[AB]{1,2}", null, Match("^[AB]{2,4}$"));
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Reason.ShouldEqual(RegexContainment.AmbiguousPatternReason);
    }

    [Fact]
    void should_choose_the_shortest_then_ordinal_counterexample()
    {
        var result = FiniteGenerationRules.Check(GenerationPattern.Parse("[AB]{1,2}[AB]{1,2}"), Match("^[AB]{2}$"));
        result.Status.ShouldEqual(RuleConsistencyStatus.Refuted);
        result.Witness.ShouldEqual("AAA");
    }

    [Fact]
    void should_keep_full_authored_runtime_ambiguity_unproven()
    {
        var result = FiniteGenerationRules.Check(GenerationPattern.Parse("[AB]{1,2}"), Match("^[AB]{1,2}$|^(a|aa)+$"));
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Reason.ShouldEqual(RegexContainment.AmbiguousPatternReason);
    }

    [Fact]
    void should_keep_nullable_repetition_unproven() => FiniteGenerationRules.Check(GenerationPattern.Parse("[A]"), Match("^(A?)*$")).Status.ShouldEqual(RuleConsistencyStatus.Unproven);

    [Fact]
    void should_never_refute_from_an_impossible_date_shape_witness()
    {
        var result = FiniteGenerationRules.Check(GenerationPattern.Parse("{yyyy}[AB]"), Match("^2026[AB]$"), new(2026, 1, 1));
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Witness.ShouldBeNull();
    }

    [Fact]
    void should_never_refute_from_an_impossible_check_digit()
    {
        var result = FiniteGenerationRules.Check(GenerationPattern.Parse("[AB]", "luhn"), Match("^[AB]0$"));
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Witness.ShouldBeNull();
    }

    [Fact]
    void should_refute_an_approximate_shape_only_with_genuine_support()
    {
        var result = FiniteGenerationRules.Check(GenerationPattern.Parse("[AB]", "luhn"), Match("^[AB]9$"));
        result.Status.ShouldEqual(RuleConsistencyStatus.Refuted);
        result.Witness.ShouldEqual("A0");
    }

    [Fact]
    void should_prove_a_same_algorithm_checksum_by_construction() => FiniteGenerationRules.CheckChecksum(GenerationPattern.Parse("[AB]", "luhn"), "luhn").Status.ShouldEqual(RuleConsistencyStatus.Proven);

    [Fact]
    void should_decide_numeric_intervals_without_floating_point_rounding()
    {
        var rule = new SemanticValidationRule(default, SemanticValidationRuleKind.Maximum, SemanticValue.Number(9007199254740992m), null);
        var result = RuleConsistency.CheckRandom("9007199254740992..9007199254740993", rule);
        result.Status.ShouldEqual(RuleConsistencyStatus.Refuted);
        result.Witness.ShouldEqual("9007199254740993");
    }

    [Fact]
    void should_refuse_whitespace_only_generated_text()
    {
        var result = FiniteGenerationRules.Check(GenerationPattern.Parse("[ ]{1,2}"), new(default, SemanticValidationRuleKind.NotEmpty, null, null));
        result.Status.ShouldEqual(RuleConsistencyStatus.Refuted);
        result.Witness.ShouldEqual(" ");
    }

    [Fact]
    void should_refute_a_real_sample_even_after_the_containment_product_budget_is_exceeded()
    {
        const string Regex = "^[AB]*A[AB]{13}$";
        var pattern = GenerationPattern.Parse("[AB]{64}");
        var containment = FinitePatternContainment.Check(pattern, new RegexFragmentR(Regex, pattern.MaximumLength).Parse());
        containment.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        containment.Reason.ShouldEqual(new RegexStateBudgetExceeded().Message);
        var result = FiniteGenerationRules.Check(pattern, Match(Regex));
        result.Status.ShouldEqual(RuleConsistencyStatus.Refuted);
        result.Witness.ShouldEqual(new string('B', 64));
    }

    static SemanticValidationRule Match(string pattern) => new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation.for_RuleConsistency;

public class when_bounding_stack_usage : Specification
{
    [Theory]
    [InlineData(SemanticValidationRuleKind.Equal)]
    [InlineData(SemanticValidationRuleKind.NotEqual)]
    [InlineData(SemanticValidationRuleKind.Matches)]
    void should_return_a_budget_outcome_for_a_long_flat_operand(SemanticValidationRuleKind kind)
    {
        var operand = new string('a', 100000);
        if (kind == SemanticValidationRuleKind.Matches) operand = "^" + operand + "$";
        var result = RuleConsistency.Check(new("nanoid") { Size = 21 }, SemanticPrimitiveType.Text, new(default, kind, SemanticValue.Text(operand), null));
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Reason.ShouldEqual("position budget exceeded");
    }

    [Fact]
    void should_construct_a_deep_flat_tree_that_fits_the_position_budget()
    {
        var expression = RegexExpression.Empty;
        for (var index = 0; index < 100000; index++) expression = RegexExpression.Concat(expression, RegexExpression.Empty);
        expression = RegexExpression.Concat(expression, RegexExpression.Character("a"));
        expression.MinimumLength.ShouldEqual(1);
        var automaton = new GlushkovAutomaton(expression);
        automaton.Accepts(automaton.Move([0], 'a')).ShouldBeTrue();
    }

    [Theory]
    [InlineData("a")]
    [InlineData("(?=a)a")]
    void should_bound_nested_groups_before_runtime_regex_construction(string inner)
    {
        var pattern = "^" + new string('(', 10000) + inner + new string(')', 10000) + "$";
        var result = RuleConsistency.Check(new("nanoid") { Size = 21 }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null));
        result.Status.ShouldEqual(RuleConsistencyStatus.Unproven);
        result.Reason.ShouldEqual("group nesting budget exceeded");
    }

    [Fact]
    void should_admit_exactly_the_group_nesting_budget()
    {
        var pattern = "^" + new string('(', RegexFragmentR.GroupNestingBudget) + "[ab]{21}" + new string(')', RegexFragmentR.GroupNestingBudget) + "$";
        RuleConsistency.Check(new("nanoid") { Size = 21, Alphabet = "ab" }, SemanticPrimitiveType.Text, new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text(pattern), null)).Status.ShouldEqual(RuleConsistencyStatus.Proven);
    }
}

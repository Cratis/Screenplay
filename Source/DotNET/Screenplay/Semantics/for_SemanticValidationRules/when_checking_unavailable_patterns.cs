// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_unavailable_patterns : Specification
{
    SemanticRuleCheck _refused;
    SemanticRuleCheck _invalid;
    SemanticRuleCheck _absent;

    void Because()
    {
        _refused = SemanticValidationRules.Check(new SemanticValidationRule(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("(?=a)a"), null), SemanticValue.Text("a"));
        _invalid = SemanticValidationRules.Check(new SemanticValidationRule(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("("), null), SemanticValue.Text("a"));
        _absent = SemanticValidationRules.Check(_refused.Rule, SemanticValue.Null);
    }

    [Fact] void should_not_evaluate_a_refused_pattern() => _refused.Status.ShouldEqual(SemanticRuleStatus.NotEvaluable);
    [Fact] void should_carry_the_refusal_details() => _refused.Pattern.ShouldEqual(PortableMatchPattern.Check("(?=a)a"));
    [Fact] void should_use_the_refusal_reason() => _refused.Message.ShouldEqual(_refused.Pattern!.Description);
    [Fact] void should_not_evaluate_invalid_syntax() => _invalid.Status.ShouldEqual(SemanticRuleStatus.NotEvaluable);
    [Fact] void should_carry_invalid_syntax_details() => _invalid.Pattern.ShouldEqual(PortableMatchPattern.Check("("));
    [Fact] void should_not_hide_an_unavailable_pattern_behind_absence() => _absent.Status.ShouldEqual(SemanticRuleStatus.NotEvaluable);
}

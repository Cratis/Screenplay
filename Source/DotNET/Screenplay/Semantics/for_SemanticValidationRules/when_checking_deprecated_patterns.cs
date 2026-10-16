// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_deprecated_patterns : Specification
{
    SemanticValidationRule _rule;
    SemanticRuleCheck _satisfied;
    SemanticRuleCheck _failed;
    SemanticRuleCheck _absent;

    void Establish() => _rule = new(default, SemanticValidationRuleKind.Matches, SemanticValue.Text(@"^(?![\s\S]*Ab)"), null);

    void Because()
    {
        _satisfied = SemanticValidationRules.Check(_rule, SemanticValue.Text("ab"));
        _failed = SemanticValidationRules.Check(_rule, SemanticValue.Text("xAbx"));
        _absent = SemanticValidationRules.Check(_rule, SemanticValue.Null);
    }

    [Fact] void should_evaluate_the_replacement_ordinally() => _satisfied.Status.ShouldEqual(SemanticRuleStatus.Satisfied);
    [Fact] void should_reject_the_matching_literal() => _failed.Status.ShouldEqual(SemanticRuleStatus.Failed);
    [Fact] void should_accept_absence() => _absent.Status.ShouldEqual(SemanticRuleStatus.Satisfied);
    [Fact] void should_keep_the_authored_rule() => _failed.Rule.ShouldBeSame(_rule);
    [Fact] void should_expose_the_replacement() => _failed.Pattern!.Replacement.ShouldEqual(new PortableMatchReplacement(SemanticValidationRuleKind.NotContains, "Ab"));
    [Fact] void should_report_deprecation_without_blocking_evaluation() => _failed.Pattern!.Status.ShouldEqual(PortableMatchStatus.Deprecated);
    [Fact] void should_describe_the_literal_rule() => _failed.Message.ShouldEqual("A value must not contain 'Ab'.");
}

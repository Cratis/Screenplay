// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_opaque_rules : Specification
{
    SemanticRuleCheck[] _results;

    void Because() => _results = [.. new[] { SemanticValidationRuleKind.RulePredicate, SemanticValidationRuleKind.CodeValidation, SemanticValidationRuleKind.Unknown }
        .SelectMany(kind => new[] { SemanticValue.Text("value"), SemanticValue.Null }
            .Select(value => SemanticValidationRules.Check(new SemanticValidationRule(default, kind, null, "Authored refusal") { Name = "Allowed", RequirementId = "rule-1" }, value)))];

    [Fact] void should_not_treat_absence_as_proof() => _results.ShouldEachConformTo(check => check.Status == SemanticRuleStatus.NotEvaluable);
    [Fact] void should_explain_why_the_rule_cannot_run() => _results.ShouldEachConformTo(check => check.Message == $"Validation rule '{check.Rule.Kind}' is not executable by the reference evaluator.");
    [Fact] void should_preserve_requirement_identity() => _results.ShouldEachConformTo(check => check.Rule.RequirementId == "rule-1");
    [Fact] void should_not_invent_pattern_details() => _results.ShouldEachConformTo(check => check.Pattern is null);
}

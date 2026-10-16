// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_satisfied_rules : given.rule_examples
{
    SemanticRuleCheck[] _results;

    void Because() => _results = [.. Examples.Select(example => SemanticValidationRules.Check(example.Rule, example.Satisfied))];

    [Fact] void should_cover_every_executable_rule_kind() => Examples.Select(example => example.Rule.Kind).ShouldEqual(Enum.GetValues<SemanticValidationRuleKind>().Where(kind => kind is not (SemanticValidationRuleKind.Unknown or SemanticValidationRuleKind.RulePredicate or SemanticValidationRuleKind.CodeValidation)));
    [Fact] void should_satisfy_every_rule() => _results.ShouldEachConformTo(check => check.Status == SemanticRuleStatus.Satisfied);
    [Fact] void should_retain_the_original_rules() => _results.Select(check => check.Rule).ShouldEqual(Examples.Select(example => example.Rule));
    [Fact] void should_not_invent_a_concept_identity() => _results.ShouldEachConformTo(check => check.Concept is null);
}

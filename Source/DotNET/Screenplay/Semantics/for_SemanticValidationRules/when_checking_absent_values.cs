// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_absent_values : given.rule_examples
{
    SemanticRuleCheck[] _results;

    void Because() => _results = [.. Examples.Select(example => SemanticValidationRules.Check(example.Rule, SemanticValue.Null))];

    [Fact] void should_leave_presence_to_not_empty() => _results.Where(check => check.Rule.Kind != SemanticValidationRuleKind.NotEmpty).ShouldEachConformTo(check => check.Status == SemanticRuleStatus.Satisfied);
    [Fact] void should_fail_not_empty() => _results.Single(check => check.Rule.Kind == SemanticValidationRuleKind.NotEmpty).Status.ShouldEqual(SemanticRuleStatus.Failed);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_failed_rules : given.rule_examples
{
    SemanticRuleCheck[] _results;

    void Because() => _results = [.. Examples.Select(example => SemanticValidationRules.Check(example.Rule, example.Failed))];

    [Fact] void should_fail_every_rule() => _results.ShouldEachConformTo(check => check.Status == SemanticRuleStatus.Failed);
    [Fact] void should_describe_each_failure() => _results.Select(check => check.Message).ShouldEqual([
        "A required value is empty.",
        "A value must be at most 2.",
        "A value must be at least 2.",
        "A value must equal 'Ab'.",
        "A value must not equal 'Ab'.",
        "A value must be greater than 2.",
        "A value must be at least 2.",
        "A value must be less than 2.",
        "A value must be at most 2.",
        "A value must be exactly 2 characters long.",
        "Every value must be greater than 2.",
        "Every value must be at least 2.",
        "A value must match the required pattern.",
        "A value must end in the exact 'luhn' check digits.",
        "A value must contain 'Ab'.",
        "A value must not contain 'Ab'.",
        "A value must start with 'Ab'.",
        "A value must end with 'Ab'.",
        "A value must have at most 2 decimal places."]);
}

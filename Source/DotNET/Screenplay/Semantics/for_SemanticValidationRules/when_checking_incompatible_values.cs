// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_incompatible_values : Specification
{
    Exception[] _errors;

    void Because() => _errors = [.. new[]
    {
        new SemanticValidationRule(default, SemanticValidationRuleKind.Length, SemanticValue.Number(2), null),
        new SemanticValidationRule(default, SemanticValidationRuleKind.Contains, SemanticValue.Text("a"), null),
        new SemanticValidationRule(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("^a$"), null),
        new SemanticValidationRule(default, SemanticValidationRuleKind.Matches, SemanticValue.Text("(?=a)a"), null),
        new SemanticValidationRule(default, SemanticValidationRuleKind.Checksum, SemanticValue.Text("luhn"), null),
        new SemanticValidationRule(default, SemanticValidationRuleKind.AllGreaterThan, SemanticValue.Number(2), null),
        new SemanticValidationRule(default, SemanticValidationRuleKind.Maximum, SemanticValue.Text("two"), null)
    }.Select(rule => Catch.Exception(() => SemanticValidationRules.Check(rule, SemanticValue.Number(2))))];

    [Fact] void should_report_contract_errors_rather_than_rule_failures() => _errors.ShouldEachConformTo(error => error is InvalidSemanticContract);
}

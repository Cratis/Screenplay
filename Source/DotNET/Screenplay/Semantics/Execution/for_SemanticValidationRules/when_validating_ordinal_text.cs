// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticValidationRules;

public class when_validating_ordinal_text : Specification
{
    static readonly SemanticValidationRuleKind[] Kinds = [SemanticValidationRuleKind.Contains, SemanticValidationRuleKind.NotContains, SemanticValidationRuleKind.StartsWith, SemanticValidationRuleKind.EndsWith];
    static readonly string[] Values = ["", "A", "AB", "BA", "A\n", "é雪", "e\u0301", "\ud800", "😀", "\0A"];
    bool _agrees;

    void Because() => _agrees = Kinds.All(kind => Values.All(operand => Values.All(value =>
        SemanticValidationRules.Satisfies(Rule(kind, operand), SemanticValue.Text(value)) == Expected(kind, value, operand))));

    [Fact] void should_compare_every_code_unit_ordinally() => _agrees.ShouldBeTrue();
    [Fact] void should_execute_every_new_kind() => Kinds.ShouldEachConformTo(SemanticValidationRules.Evaluates);
    [Fact] void should_leave_presence_to_not_empty() => Kinds.ShouldEachConformTo(kind => SemanticValidationRules.Satisfies(Rule(kind, ""), SemanticValue.Null));
    [Fact] void should_accept_empty_contains() => SemanticValidationRules.Satisfies(Rule(SemanticValidationRuleKind.Contains, ""), SemanticValue.Text("text")).ShouldBeTrue();
    [Fact] void should_reject_empty_not_contains() => SemanticValidationRules.Satisfies(Rule(SemanticValidationRuleKind.NotContains, ""), SemanticValue.Text("text")).ShouldBeFalse();

    static SemanticValidationRule Rule(SemanticValidationRuleKind kind, string operand) => new(default, kind, SemanticValue.Text(operand), null);

    static bool Expected(SemanticValidationRuleKind kind, string value, string operand) => kind switch
    {
        SemanticValidationRuleKind.Contains => value.Contains(operand, StringComparison.Ordinal),
        SemanticValidationRuleKind.NotContains => !value.Contains(operand, StringComparison.Ordinal),
        SemanticValidationRuleKind.StartsWith => value.StartsWith(operand, StringComparison.Ordinal),
        _ => value.EndsWith(operand, StringComparison.Ordinal)
    };
}

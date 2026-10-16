// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticValidationRules;

public class when_checking_incompatible_numeric_kinds : Specification
{
    Exception[] _ordering;
    Exception[] _quantified;

    void Because()
    {
        _ordering = [.. new[]
        {
            SemanticValidationRuleKind.GreaterThan, SemanticValidationRuleKind.GreaterThanOrEqual,
            SemanticValidationRuleKind.LessThan, SemanticValidationRuleKind.LessThanOrEqual
        }.Select(kind => Catch.Exception(() => SemanticValidationRules.Check(new SemanticValidationRule(default, kind, SemanticValue.Number(2), null), SemanticValue.Text("abc"))))];
        SemanticValue[] values =
        [
            SemanticValue.Array([SemanticValue.Text("abc")]),
            SemanticValue.Array([SemanticValue.Number(0), SemanticValue.Boolean(true)]),
            SemanticValue.Array([SemanticValue.Null])
        ];
        _quantified = [.. new[] { SemanticValidationRuleKind.AllGreaterThan, SemanticValidationRuleKind.AllGreaterThanOrEqual }
            .SelectMany(kind => values.Select(value => Catch.Exception(() => SemanticValidationRules.Check(new SemanticValidationRule(default, kind, SemanticValue.Number(2), null), value))))];
    }

    [Fact] void should_not_order_text_by_its_length() => _ordering.ShouldEachConformTo(error => error is InvalidSemanticContract);
    [Fact] void should_reject_every_non_numeric_element_even_after_a_failed_comparison() => _quantified.ShouldEachConformTo(error => error is InvalidSemanticContract);
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_checking_calendar_date_ordering : Specification
{
    SemanticRuleCheck[] _results;

    void Because() => _results = new[]
    {
        SemanticValidationRuleKind.Equal,
        SemanticValidationRuleKind.NotEqual,
        SemanticValidationRuleKind.GreaterThan,
        SemanticValidationRuleKind.GreaterThanOrEqual,
        SemanticValidationRuleKind.LessThan,
        SemanticValidationRuleKind.LessThanOrEqual
    }.Select(kind => SemanticDateComparisons.Check(new(default, kind, SemanticValue.Text("2026-01-01"), null), SemanticValue.Text("2026-01-02"), SemanticPrimitiveType.Date)).ToArray();

    [Fact] void should_apply_each_comparison_to_calendar_dates() => _results.Select(result => result.Status).ShouldEqual(new[] { SemanticRuleStatus.Failed, SemanticRuleStatus.Satisfied, SemanticRuleStatus.Satisfied, SemanticRuleStatus.Satisfied, SemanticRuleStatus.Failed, SemanticRuleStatus.Failed });
    [Fact] void should_use_the_reference_default_message() => _results[0].Message.ShouldEqual("A value must equal '2026-01-01'.");
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticConditionEvaluator;

public class when_evaluating_text_and_membership : Specification
{
    static readonly Dictionary<SemanticId, SemanticValue> _none = [];

    static SemanticValue Features(params string[] values) => new SemanticArrayValue([.. values.Select(value => SemanticValue.Text(value))]);

    static bool Test(SemanticComparisonOperator op, SemanticValue left, string right) =>
        SemanticConditionEvaluator.Evaluate(new SemanticComparison(new(default, left), op, new(default, SemanticValue.Text(right))), _none);

    [Fact] void should_find_an_element() => Test(SemanticComparisonOperator.Contains, Features("Reports", "DataQuality"), "DataQuality").ShouldBeTrue();
    [Fact] void should_not_find_a_missing_element() => Test(SemanticComparisonOperator.Contains, Features("Reports"), "DataQuality").ShouldBeFalse();
    [Fact] void should_match_an_element_exactly_not_as_a_substring() => Test(SemanticComparisonOperator.Contains, Features("DataQualityPlus"), "DataQuality").ShouldBeFalse();
    [Fact] void should_not_find_anything_in_an_empty_collection() => Test(SemanticComparisonOperator.Contains, Features(), "DataQuality").ShouldBeFalse();
    [Fact] void should_find_a_substring() => Test(SemanticComparisonOperator.Contains, SemanticValue.Text("professional"), "fess").ShouldBeTrue();
    [Fact] void should_be_case_sensitive_for_a_substring() => Test(SemanticComparisonOperator.Contains, SemanticValue.Text("professional"), "FESS").ShouldBeFalse();
    [Fact] void should_match_a_prefix() => Test(SemanticComparisonOperator.StartsWith, SemanticValue.Text("professional"), "pro").ShouldBeTrue();
    [Fact] void should_not_match_a_prefix_elsewhere() => Test(SemanticComparisonOperator.StartsWith, SemanticValue.Text("professional"), "fessional").ShouldBeFalse();
    [Fact] void should_never_match_an_absent_value() => Test(SemanticComparisonOperator.StartsWith, SemanticValue.Null, "pro").ShouldBeFalse();
    [Fact] void should_refuse_a_number_subject() => Catch.Exception(() => Test(SemanticComparisonOperator.Contains, SemanticValue.Number(1), "1")).ShouldBeOfExactType<InvalidSemanticContract>();
}

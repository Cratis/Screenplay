// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticConditionEvaluator;

public class when_evaluating_logical_conditions : Specification
{
    bool[] _results;

    void Because()
    {
        var yes = new SemanticComparison(new(default, SemanticValue.Number(1)), SemanticComparisonOperator.Equal, new(default, SemanticValue.Number(1)));
        var no = yes with { Operator = SemanticComparisonOperator.NotEqual };
        SemanticLogicalCondition[] conditions =
        [
            new(yes, SemanticLogicalOperator.And, yes),
            new(yes, SemanticLogicalOperator.And, no),
            new(no, SemanticLogicalOperator.Or, yes),
            new(no, SemanticLogicalOperator.Or, no),
            new(new SemanticLogicalCondition(no, SemanticLogicalOperator.Or, yes), SemanticLogicalOperator.And, yes)
        ];
        _results = conditions.Select(condition => SemanticConditionEvaluator.Evaluate(condition, new Dictionary<SemanticId, SemanticValue>())).ToArray();
    }

    [Fact] void should_preserve_boolean_grouping() => _results.ShouldEqual(new[] { true, false, true, false, true });
}

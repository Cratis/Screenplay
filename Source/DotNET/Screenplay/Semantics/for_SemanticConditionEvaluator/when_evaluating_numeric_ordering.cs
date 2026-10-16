// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticConditionEvaluator;

public class when_evaluating_numeric_ordering : Specification
{
    bool[] _results;

    void Because() => _results = Enum.GetValues<SemanticComparisonOperator>().Select(operation =>
        SemanticConditionEvaluator.Evaluate(new SemanticComparison(new(default, SemanticValue.Number(2)), operation, new(default, SemanticValue.Number(1))), new Dictionary<SemanticId, SemanticValue>())).ToArray();

    [Fact] void should_use_numeric_comparison() => _results.ShouldEqual(new[] { false, true, true, true, false, false });
}

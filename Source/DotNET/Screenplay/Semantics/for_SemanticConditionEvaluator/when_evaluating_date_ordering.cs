// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticConditionEvaluator;

public class when_evaluating_date_ordering : Specification
{
    bool _result;

    void Because()
    {
        var property = SemanticId.Create(SemanticKind.Property, "date");
        var comparison = new SemanticComparison(new(property, null), SemanticComparisonOperator.GreaterThan, new(default, SemanticValue.Text("2026-01-01T00:00:00.0000000Z")));
        var condition = new SemanticLogicalCondition(comparison, SemanticLogicalOperator.And, comparison);
        _result = SemanticConditionEvaluator.Evaluate(condition,
            new Dictionary<SemanticId, SemanticValue> { [property] = SemanticValue.Text("2026-01-01T02:00:00.0000001+02:00") },
            new Dictionary<SemanticId, SemanticPrimitiveType> { [property] = SemanticPrimitiveType.DateTime });
    }

    [Fact] void should_forward_date_types_through_logical_nodes_and_preserve_ticks() => _result.ShouldBeTrue();
}

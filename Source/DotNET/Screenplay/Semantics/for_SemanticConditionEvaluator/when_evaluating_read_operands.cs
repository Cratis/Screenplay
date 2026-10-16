// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticConditionEvaluator;

public class when_evaluating_read_operands : Specification
{
    bool _result;

    void Because()
    {
        var property = SemanticId.Create(SemanticKind.Property, "limit");
        var comparison = new SemanticComparison(new(default, SemanticValue.Number(2)), SemanticComparisonOperator.LessThan, new(property, null) { Read = "policy" });
        var condition = new SemanticLogicalCondition(comparison, SemanticLogicalOperator.Or, comparison);
        _result = SemanticConditionEvaluator.Evaluate(condition, new Dictionary<SemanticId, SemanticValue>(), reads:
            new Dictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>> { ["policy"] = new Dictionary<SemanticId, SemanticValue> { [property] = SemanticValue.Number(3) } });
    }

    [Fact] void should_resolve_the_named_read_through_logical_nodes() => _result.ShouldBeTrue();
}

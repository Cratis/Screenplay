// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticConditionEvaluator;

public class when_evaluating_without_date_types : Specification
{
    bool _result;

    void Because() => _result = SemanticConditionEvaluator.Evaluate(
        new SemanticComparison(new(SemanticId.Create(SemanticKind.Property, "instant"), null), SemanticComparisonOperator.Equal, new(default, SemanticValue.Text("2026-01-01T00:00:00Z"))),
        new Dictionary<SemanticId, SemanticValue> { [SemanticId.Create(SemanticKind.Property, "instant")] = SemanticValue.Text("2026-01-01T02:00:00+02:00") });

    [Fact] void should_keep_text_equality_ordinal() => _result.ShouldBeFalse();
}

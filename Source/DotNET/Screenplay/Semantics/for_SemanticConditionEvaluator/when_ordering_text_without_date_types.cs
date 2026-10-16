// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticConditionEvaluator;

public class when_ordering_text_without_date_types : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => SemanticConditionEvaluator.Evaluate(
        new SemanticComparison(new(default, SemanticValue.Text("2026-01-02")), SemanticComparisonOperator.GreaterThan, new(default, SemanticValue.Text("2026-01-01"))), new Dictionary<SemanticId, SemanticValue>()));

    [Fact] void should_refuse_non_numeric_ordering() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_preserve_the_reference_message() => _error.Message.ShouldEqual("Ordering condition requires numeric operands.");
}

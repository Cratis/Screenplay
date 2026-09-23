// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

public class and_a_collection_holds_a_number_outside_its_bound : given.a_validated_command_plan
{
    SemanticRejected _notGreater;
    SemanticRejected _belowInclusive;
    SemanticExecutionResult _empty;

    void Because()
    {
        _notGreater = (SemanticRejected)Execute(("weights", SemanticValue.Array([SemanticValue.Number(1), SemanticValue.Number(0)])));
        _belowInclusive = (SemanticRejected)Execute(("weights", SemanticValue.Array([SemanticValue.Number(1), SemanticValue.Number(0.25m)])));
        _empty = Execute(("weights", SemanticValue.Array([])));
    }

    [Fact] void should_reject_an_element_not_greater_than_the_operand() => _notGreater.Details.ShouldEqual("Every weight counts");
    [Fact] void should_reject_an_element_below_the_inclusive_bound() => _belowInclusive.Details.ShouldEqual("No weight is below a half");
    [Fact] void should_report_validation() => _notGreater.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_accept_an_empty_collection() => _empty.ShouldBeOfExactType<SemanticAccepted>();
}

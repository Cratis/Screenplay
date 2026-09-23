// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

public class and_a_number_is_ordered_outside_its_bounds : given.a_validated_command_plan
{
    SemanticRejected _notGreater;
    SemanticRejected _belowInclusive;
    SemanticRejected _notLess;
    SemanticRejected _aboveInclusive;
    SemanticExecutionResult _atEveryInclusiveBound;
    SemanticExecutionResult _justInsideTheExclusiveBounds;

    void Because()
    {
        _notGreater = (SemanticRejected)Execute(("amount", SemanticValue.Number(0)));
        _belowInclusive = (SemanticRejected)Execute(("priority", SemanticValue.Number(0)));
        _notLess = (SemanticRejected)Execute(("priority", SemanticValue.Number(6)));
        _aboveInclusive = (SemanticRejected)Execute(("discount", SemanticValue.Number(100.5m)));
        _atEveryInclusiveBound = Execute(("priority", SemanticValue.Number(1)), ("discount", SemanticValue.Number(100)));
        _justInsideTheExclusiveBounds = Execute(("amount", SemanticValue.Number(0.01m)), ("priority", SemanticValue.Number(5)));
    }

    [Fact] void should_reject_a_number_not_greater_than_the_operand() => _notGreater.Details.ShouldEqual("An order is for something");
    [Fact] void should_reject_a_number_below_an_inclusive_lower_bound() => _belowInclusive.Details.ShouldEqual("Priority starts at one");
    [Fact] void should_reject_a_number_not_less_than_the_operand() => _notLess.Details.ShouldEqual("Priority stops at five");
    [Fact] void should_reject_a_number_above_an_inclusive_upper_bound() => _aboveInclusive.Details.ShouldEqual("A discount is at most 100");
    [Fact] void should_report_validation() => _notGreater.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_accept_numbers_at_the_inclusive_bounds() => _atEveryInclusiveBound.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_accept_numbers_just_inside_the_exclusive_bounds() => _justInsideTheExclusiveBounds.ShouldBeOfExactType<SemanticAccepted>();
}

// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

public class and_the_value_equals_a_forbidden_member : given.a_validated_command_plan
{
    SemanticRejected _rejected;

    void Because() => _rejected = (SemanticRejected)Execute(("status", SemanticValue.Text("closed")));

    [Fact] void should_report_validation() => _rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_report_the_rule_message() => _rejected.Details.ShouldEqual("A closed order cannot be placed");
}

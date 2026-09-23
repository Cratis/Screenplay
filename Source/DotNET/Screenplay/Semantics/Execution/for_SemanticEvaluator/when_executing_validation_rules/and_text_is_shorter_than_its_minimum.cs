// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

public class and_text_is_shorter_than_its_minimum : given.a_validated_command_plan
{
    SemanticRejected _rejected;
    SemanticExecutionResult _atTheBound;

    void Because()
    {
        _rejected = (SemanticRejected)Execute(("name", SemanticValue.Text("A")));
        _atTheBound = Execute(("name", SemanticValue.Text("AB")));
    }

    [Fact] void should_report_validation() => _rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_report_the_rule_message() => _rejected.Details.ShouldEqual("A name has at least two characters");
    [Fact] void should_carry_no_code() => _rejected.Code.ShouldBeNull();
    [Fact] void should_accept_text_at_the_bound() => _atTheBound.ShouldBeOfExactType<SemanticAccepted>();
}

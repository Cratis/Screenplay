// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

public class and_a_number_exceeds_its_maximum : given.a_validated_command_plan
{
    SemanticRejected _rejected;
    SemanticExecutionResult _atTheBound;

    void Because()
    {
        _rejected = (SemanticRejected)Execute(("amount", SemanticValue.Number(1000.01m)));
        _atTheBound = Execute(("amount", SemanticValue.Number(1000)));
    }

    [Fact] void should_report_the_rule_message() => _rejected.Details.ShouldEqual("An order is at most 1000");
    [Fact] void should_accept_a_number_at_the_bound() => _atTheBound.ShouldBeOfExactType<SemanticAccepted>();
}

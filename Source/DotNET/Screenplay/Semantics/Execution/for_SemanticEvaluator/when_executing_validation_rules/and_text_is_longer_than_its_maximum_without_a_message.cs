// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

public class and_text_is_longer_than_its_maximum_without_a_message : given.a_validated_command_plan
{
    SemanticRejected _rejected;
    SemanticExecutionResult _atTheBound;

    void Because()
    {
        _rejected = (SemanticRejected)Execute(("name", SemanticValue.Text("ABCDEFGHIJK")));
        _atTheBound = Execute(("name", SemanticValue.Text("ABCDEFGHIJ")));
    }

    [Fact] void should_report_validation() => _rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_describe_the_length_bound() => _rejected.Details.ShouldEqual("A value must be at most 10 characters long.");
    [Fact] void should_accept_text_at_the_bound() => _atTheBound.ShouldBeOfExactType<SemanticAccepted>();
}

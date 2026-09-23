// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

public class and_text_has_another_length : given.a_validated_command_plan
{
    SemanticRejected _shorter;
    SemanticRejected _longer;

    void Because()
    {
        _shorter = (SemanticRejected)Execute(("code", SemanticValue.Text("NO")));
        _longer = (SemanticRejected)Execute(("code", SemanticValue.Text("NOKS")));
    }

    [Fact] void should_reject_shorter_text() => _shorter.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_reject_longer_text() => _longer.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_describe_the_exact_length() => _shorter.Details.ShouldEqual("A value must be exactly 3 characters long.");
}

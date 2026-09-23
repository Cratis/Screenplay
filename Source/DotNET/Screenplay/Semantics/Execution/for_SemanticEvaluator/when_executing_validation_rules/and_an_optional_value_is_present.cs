// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

// An absent optional value satisfies every rule but not-empty; a present one is held to them.
public class and_an_optional_value_is_present : given.a_validated_command_plan
{
    SemanticRejected _rejected;
    SemanticExecutionResult _absent;

    void Because()
    {
        _rejected = (SemanticRejected)Execute(("note", SemanticValue.Text("ab")));
        _absent = Execute(("note", SemanticValue.Null));
    }

    [Fact] void should_hold_a_present_value_to_its_rule() => _rejected.Details.ShouldEqual("A note has at least three characters");
    [Fact] void should_accept_an_absent_value() => _absent.ShouldBeOfExactType<SemanticAccepted>();
}

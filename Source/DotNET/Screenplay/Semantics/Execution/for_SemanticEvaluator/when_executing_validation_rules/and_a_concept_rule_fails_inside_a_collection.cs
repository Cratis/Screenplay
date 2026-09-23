// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

// A concept's rules constrain every value of the concept, including each element of a collection.
public class and_a_concept_rule_fails_inside_a_collection : given.a_validated_command_plan
{
    SemanticRejected _rejected;
    SemanticExecutionResult _empty;

    void Because()
    {
        _rejected = (SemanticRejected)Execute(("quantities", SemanticValue.Array([SemanticValue.Number(1), SemanticValue.Number(0)])));
        _empty = Execute(("quantities", SemanticValue.Array([])));
    }

    [Fact] void should_report_validation() => _rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_report_the_concept_rule_message() => _rejected.Details.ShouldEqual("Order at least one");
    [Fact] void should_accept_a_collection_with_no_values() => _empty.ShouldBeOfExactType<SemanticAccepted>();
}

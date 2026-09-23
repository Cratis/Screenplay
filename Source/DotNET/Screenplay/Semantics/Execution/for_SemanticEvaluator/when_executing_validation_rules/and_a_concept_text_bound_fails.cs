// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_executing_validation_rules;

public class and_a_concept_text_bound_fails : given.a_validated_command_plan
{
    SemanticRejected _rejected;

    void Because() => _rejected = (SemanticRejected)Execute(("reference", SemanticValue.Text("REF-123456789")));

    [Fact] void should_report_validation() => _rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_report_the_concept_rule_message() => _rejected.Details.ShouldEqual("A reference is at most twelve characters");
}

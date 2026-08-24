// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_executing_a_rejected_command : a_valid_semantic_model
{
    SemanticWorld _before;
    SemanticRejected _rejected;

    void Because()
    {
        var plan = SemanticExecutionPlan.Compile(_model).Plan!;
        var specification = plan.Specifications.Values.Single(_ => _.ThenErrors.Length > 0);
        var request = SemanticExecutionRequest.Create(specification.When.Command, specification.When.Values, []);
        _before = SemanticWorld.Empty;
        _rejected = (SemanticRejected)new SemanticEvaluator().Execute(plan, _before, request);
    }

    [Fact] void should_reject_the_command() => _rejected.Kind.ShouldEqual(SemanticExecutionOutcomeKind.Rejected);
    [Fact] void should_report_validation() => _rejected.Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_report_the_expected_message() => _rejected.Details.ShouldEqual("Project name is required");
    [Fact] void should_keep_the_original_world() => ReferenceEquals(_rejected.World, _before).ShouldBeTrue();
    [Fact] void should_append_no_fact() => _rejected.World.Facts.ShouldBeEmpty();
    [Fact] void should_change_no_read_model() => _rejected.World.ReadModels.ShouldBeEmpty();
}
